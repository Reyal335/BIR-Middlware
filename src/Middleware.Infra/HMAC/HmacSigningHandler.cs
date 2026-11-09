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

/// <summary>
/// SigningKey keys the HMAC. BearerToken, when set, is sent as "authorization: Bearer {token}";
/// it is null for the auth call, which happens before a token exists.
/// </summary>
public sealed record HmacCredentials(string SigningKey, string? BearerToken);

public class HmacSigningHandler (
    IOptions<EisOptions> options,
    Func<CancellationToken, ValueTask<HmacCredentials>> resolveCredentials) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        // Differs per client: ApplicationSecretKey and no token for EisAuth,
        // SessionKey and AuthToken from the current session for Eis
        HmacCredentials credentials = await resolveCredentials(cancellationToken);
        string signingKey = credentials.SigningKey;

        string manilaTimezone = "Asia/Manila";
        TimeZoneInfo manilaZone = TimeZoneInfo.FindSystemTimeZoneById(manilaTimezone);
        DateTime manilaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, manilaZone);

        // Date formatted to yyyyMMddHHmmss as per BIR regulations
        string dateMessage = $"{manilaTime:yyyyMMddHHmmss}";
        
        string message = $"{dateMessage}/{request.Method.Method}/{request.RequestUri!.AbsolutePath}";

        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        byte[] keyBytes = Encoding.UTF8.GetBytes(signingKey);

        byte[] hashBytes = HMACSHA256.HashData(keyBytes, messageBytes);
        string messageEnoded = System.Convert.ToBase64String(hashBytes);

        request.Headers.Add("datetime", dateMessage);
        request.Headers.Add("accreditationId", options.Value.AccreditationId);
        request.Headers.Add("applicationId", options.Value.ApplicationId);
        request.Headers.Add("signature", messageEnoded);

        if (credentials.BearerToken is not null)
            request.Headers.Add("authorization", $"Bearer {credentials.BearerToken}");

        // This is the one that sends the request to the EisAuth
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        return response;
    }
}