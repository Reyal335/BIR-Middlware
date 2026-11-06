using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Eis.Mock.Store;
using Middleware.Core.Eis;
using Middleware.Infra.Eis;

namespace Eis.Mock.Tests;

/// <summary>Builds issuance requests the way the middleware does.</summary>
public static class IssuanceKit
{
    public const string Path = "/api/v2/invoices";
    public const string CertificationNumber = "ABCD1234";
    public const string RegisteredTin = "123456789";

    public static DateTimeOffset ManilaNow => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8));

    public static (string Token, string SessionKey) AddSession(SessionStore sessions, TimeSpan? validFor = null)
    {
        string token = RandomNumberGenerator.GetString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 32);
        string sessionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        DateTimeOffset expiry = ManilaNow.Add(validFor ?? TimeSpan.FromHours(6));

        sessions.AddSession(token, new SessionDetails(sessionKey, $"{expiry:yyyy-MM-ddTHH:mm:ss}"));
        return (token, sessionKey);
    }

    public static string NewSessionKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public static string NewSubmitId() => SubmitIdGenerator.Create(CertificationNumber, ManilaNow);

    public static string NewUniqueId(DateTimeOffset issuedAt) =>
        $"{issuedAt:yyyyMMdd}{CertificationNumber}{RandomNumberGenerator.GetInt32(0, 100_000_000):00000000}";

    /// <summary>A VATABLE single-line invoice that passes every per-invoice check.</summary>
    public static BirEisInvoicePayload ValidInvoice(DateTimeOffset? issuedAt = null, string? uniqueId = null)
    {
        DateTimeOffset issued = issuedAt ?? ManilaNow.AddMinutes(-5);
        return new BirEisInvoicePayload
        {
            EisUniqueId = uniqueId ?? NewUniqueId(issued),
            DocumentType = "SALES_INVOICE",
            Header = new EisHeader
            {
                InvoiceNumber = "INV-000001",
                InvoiceDate = issued.ToString("yyyy-MM-dd"),
                InvoiceTime = issued.ToString("HH:mm:sszzz"),
                Currency = "PHP",
            },
            Seller = new EisParty
            {
                Tin = RegisteredTin,
                BranchCode = "00000",
                RegisteredName = "Test Seller Inc.",
                VatRegistration = "VAT",
            },
            Buyer = new EisParty { RegisteredName = "WALK_IN CUSTOMER", BuyerType = "WALK_IN" },
            ItemList =
            [
                new EisLineItem
                {
                    LineNo = 1, Name = "SKU-1", Qty = 1m, UnitCost = 112.00m, Discount = 0m,
                    SalesAmt = 112.00m, NetSales = 112.00m, TaxType = "VATABLE",
                },
            ],
            TaxSummary = new EisTaxSummary { VatableSales = 100.00m, VatAmount = 12.00m, TotalAmountDue = 112.00m },
        };
    }

    public static string Sign(EisSigner signer, BirEisInvoicePayload invoice) => signer.Sign(EisJson.Serialize(invoice));

    /// <summary>
    /// Same layout as the middleware's EisSigner.Encrypt (Base64 key, nonce || ciphertext || tag),
    /// except the plaintext is UTF-8 encoded rather than Base64-decoded.
    /// </summary>
    public static string Encrypt(string plainText, string base64Key)
    {
        byte[] plain = Encoding.UTF8.GetBytes(plainText);
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] cipher = new byte[plain.Length];
        byte[] tag = new byte[16];

        using var aes = new AesGcm(Convert.FromBase64String(base64Key), tag.Length);
        aes.Encrypt(nonce, plain, cipher, tag);

        return Convert.ToBase64String([.. nonce, .. cipher, .. tag]);
    }

    public static string Decrypt(string base64Cipher, string base64Key)
    {
        byte[] input = Convert.FromBase64String(base64Cipher);
        byte[] plain = new byte[input.Length - 28];

        using var aes = new AesGcm(Convert.FromBase64String(base64Key), 16);
        aes.Decrypt(input.AsSpan(0, 12), input.AsSpan(12, plain.Length), input.AsSpan(12 + plain.Length, 16), plain);

        return Encoding.UTF8.GetString(plain);
    }

    public static string EncryptInvoices(IEnumerable<string> jws, string sessionKey) =>
        Encrypt(JsonSerializer.Serialize(new { invoices = jws.ToArray() }), sessionKey);

    public static string Body(string submitId, string data) => JsonSerializer.Serialize(new { submitId, data });

    /// <summary>
    /// A POST to /api/v2/invoices. The signature uses HmacSigningHandler's string-to-sign; pass a null
    /// token or signing key to leave that header out.
    /// </summary>
    public static HttpRequestMessage Request(string body, string? token, string? signingKey, DateTimeOffset? datetime = null)
    {
        string dt = $"{datetime ?? ManilaNow:yyyyMMddHHmmss}";

        var request = new HttpRequestMessage(HttpMethod.Post, Path)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("datetime", dt);
        request.Headers.Add("accreditationId", "ACC-TEST-001");
        request.Headers.Add("applicationId", "APP-GAP");
        if (token is not null)
            request.Headers.TryAddWithoutValidation("authorization", $"Bearer {token}");
        if (signingKey is not null)
            request.Headers.Add("signature", Hmac(dt, signingKey));

        return request;
    }

    public static string Hmac(string datetime, string key) =>
        Convert.ToBase64String(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(key),
            Encoding.UTF8.GetBytes($"{datetime}/POST/{Path}")));

    /// <summary>Sends a fully valid request for the given JWS strings.</summary>
    public static Task<HttpResponseMessage> SubmitAsync(
        HttpClient client, (string Token, string SessionKey) session, string submitId, IEnumerable<string> jws) =>
        client.SendAsync(Request(
            Body(submitId, EncryptInvoices(jws, session.SessionKey)),
            session.Token,
            session.SessionKey));

    public static async Task AssertErrorAsync(
        HttpResponseMessage response, HttpStatusCode status, string code, string? submitId = null)
    {
        string json = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"Expected {(int)status}, got {(int)response.StatusCode}: {json}");

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(code, doc.RootElement.GetProperty("error_code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("message").GetString()));
        if (submitId is not null)
            Assert.Equal(submitId, doc.RootElement.GetProperty("submitId").GetString());
    }

    public static async Task<Submission> WaitForCompletionAsync(SubmissionStore store, string submitId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            if (store.TryGet(submitId, out var submission) && submission.Status == SubmissionStatus.COMPLETED)
                return submission;
            await Task.Delay(25);
        }
        throw new TimeoutException($"Submission {submitId} was not COMPLETED within 10 seconds.");
    }
}
