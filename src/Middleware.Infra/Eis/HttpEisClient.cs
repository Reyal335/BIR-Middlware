using Middleware.Core.interfaces;
using Middleware.Core.Models;
using Middleware.Core.Eis;
using System.Text.Json;
using Middleware.Infra.Cache;

namespace Middleware.Infra.Eis;

public class HttpEisClient(
    IHttpClientFactory httpClientFactory, 
    EisTokenCache tokenCache,
    IEisSigner eisSigner) : IEisClient
{
    public async Task SubmitBatch(List<Invoice> invoices)
    {
        /** 
            - Creates a session here if does not exist. Uses the cached session if one exists
            - Each session lasts for 6 hours
        **/
        var session = await tokenCache.GetEisSessionAsync();

        var plainText = JsonSerializer.Serialize(new
        {
            invoices = invoices.Select(i => i.SignedJws).ToArray()
        });

        var data = eisSigner.Encrypt(plainText, session.SessionKey);


    }

    public async Task GetResults()
    {
    
    }
}