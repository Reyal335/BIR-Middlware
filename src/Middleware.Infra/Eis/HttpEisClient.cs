using Middleware.Core.interfaces;
using Middleware.Core.Models;
using Middleware.Core.Eis;
using System.Text.Json;
using Middleware.Infra.Cache;
using Microsoft.Extensions.Options;
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
        var reply = await response.Content.ReadFromJsonAsync<EisAuthReply>(ct)
            ?? throw new InvalidOperationException("EIS returned an empty response body.");

        var receipt = JsonSerializer.Deserialize<EisReceipt>(eisSigner.Decrypt(reply.Data, session.SessionKey))
            ?? throw new InvalidOperationException("EIS receipt is empty.");

        if (receipt.SubmitId != submitId)
            throw new InvalidOperationException($"EIS receipt is for {receipt.SubmitId}, expected {submitId}.");

        return receipt;
    }

    public async Task GetResults()
    {
    
    }
}