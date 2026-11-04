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
    IOptions<EisOptions> eisOptions,
    IOptions<TimeZoneOptions> tzOptions
    ) : IEisClient
{
    public async Task SubmitBatch(List<Invoice> invoices, CancellationToken ct)
    {
        /** 
            - Creates a session here if does not exist. Uses the cached session if one exists
            - Each session lasts for 6 hours
        **/

        var eisOpts = eisOptions.Value;
        var tzOpts = tzOptions.Value;

        var session = await tokenCache.GetEisSessionAsync(ct);

        ct.ThrowIfCancellationRequested(); 

        var plainText = JsonSerializer.Serialize(new
        {
            invoices = invoices.Select(i => i.SignedJws).ToArray()
        });

        var data = eisSigner.Encrypt(plainText, session.SessionKey);

        
        TimeZoneInfo manilaTime = TimeZoneInfo.FindSystemTimeZoneById(tzOpts.TimeZone);
        DateTimeOffset timeNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, manilaTime);

        if(eisOpts.CertificationNumber is null)
            throw new InvalidOperationException("Certification Number cannot be null");

        // Get submitId
        string submitId = SubmitIdGenerator.Create(eisOpts.CertificationNumber, timeNow);

        var client = httpClientFactory.CreateClient("Eis");

        var response = await client.PostAsJsonAsync(eisOpts.SubmitPath, new { submitId, data });
        response.EnsureSuccessStatusCode();

    }

    public async Task GetResults()
    {
    
    }
}