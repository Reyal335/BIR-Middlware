using Middleware.Core.interfaces;
using Middleware.Core.Models;
using Middleware.Core.Eis;
using System.Text.Json;
using Middleware.Infra.Cache;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;

namespace Middleware.Infra.Eis;

public class HttpEisClient(
    IHttpClientFactory httpClientFactory, 
    EisTokenCache tokenCache,
    IEisSigner eisSigner,
    IOptions<EisOptions> eisOptions
    ) : IEisClient
{
    public async Task<EisReceipt> SubmitBatch(string submitId, List<Invoice> invoices, CancellationToken ct)
    {
        /** 
            - Creates a session here if does not exist. Uses the cached session if one exists
            - Each session lasts for 6 hours
        **/

        var eisOpts = eisOptions.Value;

        var session = await tokenCache.GetEisSessionAsync(ct);

        ct.ThrowIfCancellationRequested(); 

        var plainText = JsonSerializer.Serialize(new
        {
            invoices = invoices.Select(i => i.SignedJws).ToArray()
        });

        var data = eisSigner.Encrypt(plainText, session.SessionKey);

        if(eisOpts.CertificationNumber is null)
            throw new InvalidOperationException("Certification Number cannot be null");

        var client = httpClientFactory.CreateClient("Eis");

        var response = await client.PostAsJsonAsync(eisOpts.SubmitPath, new { submitId, data }, ct);
        response.EnsureSuccessStatusCode();

        // Body is { data }: the receipt, encrypted with the same session key as the request
        var receipt = await ReadEncryptedAsync<EisReceipt>(response, session.SessionKey, ct);

        if (receipt.SubmitId != submitId)
            throw new InvalidOperationException($"EIS receipt is for {receipt.SubmitId}, expected {submitId}.");

        return receipt;
    }

    public async Task<EisInquiryResult> GetResults(string submitId, CancellationToken ct)
    {
        var resultPath = eisOptions.Value.ResultPath;
        if (string.IsNullOrWhiteSpace(resultPath))
            throw new InvalidOperationException("Eis:ResultPath is not configured");

        var session = await tokenCache.GetEisSessionAsync(ct);

        // GET {ResultPath}/{submitId}: the submitId is in the path, so HmacSigningHandler's signature covers it
        var client = httpClientFactory.CreateClient("Eis");
        var response = await client.GetAsync($"{resultPath.TrimEnd('/')}/{Uri.EscapeDataString(submitId)}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound && await IsSubmissionNotFoundAsync(response, ct))
            throw new EisSubmissionNotFoundException(submitId);

        response.EnsureSuccessStatusCode();

        var result = await ReadEncryptedAsync<EisInquiryResult>(response, session.SessionKey, ct);

        if (result.SubmitId != submitId)
            throw new InvalidOperationException($"EIS inquiry result is for {result.SubmitId}, expected {submitId}.");

        return result;
    }

    /// <summary>Reads a { data } body and decrypts it with the session key that signed the request.</summary>
    private async Task<T> ReadEncryptedAsync<T>(HttpResponseMessage response, string sessionKey, CancellationToken ct)
    {
        var reply = await response.Content.ReadFromJsonAsync<EisAuthReply>(ct)
            ?? throw new InvalidOperationException("EIS returned an empty response body.");

        return JsonSerializer.Deserialize<T>(eisSigner.Decrypt(reply.Data, sessionKey))
            ?? throw new InvalidOperationException($"EIS returned an empty {typeof(T).Name}.");
    }

    /// <summary>
    /// True only for the EIS's own "no such submission" error (REQ006). A 404 without it means the
    /// route itself wasn't found (e.g. a wrong ResultPath), which must not be mistaken for a lost batch.
    /// </summary>
    private static async Task<bool> IsSubmissionNotFoundAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("error_code", out var code)
                && code.ValueKind == JsonValueKind.String
                && code.GetString() == SubmissionNotFoundCode;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private const string SubmissionNotFoundCode = "REQ006";
}