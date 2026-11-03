using Middleware.Infra.Eis;
using Middleware.Infra.Helpers;
using Middleware.Core.Eis;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Internal;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using System.Net.Http.Json;
using System.Globalization;


namespace Middleware.Infra.Cache;

public sealed record EisSession(string AuthToken, string SessionKey, DateTimeOffset ExpiresAt);

public class EisTokenCache(
    IHttpClientFactory httpClientFactory,
    IOptions<EisOptions> options
)
{
    private EisSession? _session;
    private readonly SemaphoreSlim _lock = new(1, 1);
    
    public async Task<EisSession> GetEisSessionAsync(CancellationToken ct = default)
    {
        // Create Session if there is no session or session is about to expire
        if (_session is null ||
            (_session.ExpiresAt - DateTimeOffset.Now).TotalMinutes < 2)
        {
            await _lock.WaitAsync();
            var ses = await Task.Run(async () =>
            {
                try
                {   
                    var client = httpClientFactory.CreateClient("EisAuth");
                    var opts = options.Value;

                    if (string.IsNullOrEmpty(opts.UserId) || string.IsNullOrEmpty(opts.Password))
                        throw new InvalidOperationException("UserId or password missing");

                    if (string.IsNullOrEmpty(opts.BirPublicKey))
                        throw new InvalidOperationException("BIR public key missing");

                    string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

                    var authKey = RandomNumberGenerator.GetString(chars, 32);
                    
                    var json = JsonSerializer.Serialize(new
                    {
                        userId = opts.UserId,
                        password = opts.Password,
                        authKey
                    });

                    using var rsa = RSA.Create();
                    rsa.ImportFromPem(opts.BirPublicKey);
                    var data = Convert.ToBase64String(
                        rsa.Encrypt(Encoding.UTF8.GetBytes(json), RSAEncryptionPadding.Pkcs1)
                    );


                    var response = await client.PostAsJsonAsync(opts.AuthPath, new { data }, ct);

                    response.EnsureSuccessStatusCode();

                    EisAuthReply? replyJson = await response.Content.ReadFromJsonAsync<EisAuthReply>();
                    // Decryption Goes Here
                    var responseJson = Aes256CbcHelper.Decrypt(replyJson!.Data, authKey);
                    EisAuthCredentials? sessionCreds = JsonSerializer.Deserialize<EisAuthCredentials>(responseJson); 

                    string manilaTimezone = "Asia/Manila";
                    TimeZoneInfo manilaZone = TimeZoneInfo.FindSystemTimeZoneById(manilaTimezone);

                    string format = "yyyy-MM-ddTHH:mm:ss";

                    bool success = DateTimeOffset.TryParseExact(
                        sessionCreds!.TokenExpiry,
                        format,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTimeOffset expiresAt
                    );

                    if (!success)
                    {
                        throw new InvalidOperationException("Error parsing datetime");
                    }

                    var session = new EisSession(sessionCreds!.AuthToken, sessionCreds!.SessionKey, expiresAt);
                    _session = session;

                    return _session;
                } 
                finally
                {
                    _lock.Release(); // Release slot
                }
            });            
        }

        return _session!;
    }

    public void Invalidate() => _session = null;
}