using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using Middleware.Infra.Eis;
using Microsoft.Extensions.Options;

namespace Middleware.Infra.HMAC;

public class HmacSigningHandler (IOptions<EisOptions> options) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        string manilaTimezone = "Asia/Manila";
        TimeZoneInfo manilaZone = TimeZoneInfo.FindSystemTimeZoneById(manilaTimezone);
        DateTime manilaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, manilaZone);

        // Date formatted to yyyyMMddHHmmss as per BIR regulations
        string dateMessage = $"{manilaTime:yyyyMMddHHmmss}";
        
        string message = $"{dateMessage}/{request.Method.Method}/{request.RequestUri!.AbsolutePath}";

        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        byte[] keyBytes = Encoding.UTF8.GetBytes(options.Value.ApplicationSecretKey);

        byte[] hashBytes = HMACSHA256.HashData(keyBytes, messageBytes);
        string messageEnoded = System.Convert.ToBase64String(hashBytes);

        request.Headers.Add("datetime", dateMessage);
        request.Headers.Add("accreditationId", options.Value.AccreditationId);
        request.Headers.Add("applicationId", options.Value.ApplicationId);
        request.Headers.Add("authorization", $"Bearer {messageEnoded}");

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        return response;
    }
}