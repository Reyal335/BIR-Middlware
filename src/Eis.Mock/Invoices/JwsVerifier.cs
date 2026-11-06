using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Eis.Mock.Invoices;

/// <summary>
/// Verifies the compact RS256 JWS produced by the middleware's EisSigner:
/// base64url(header).base64url(payload).base64url(RSASSA-PKCS1-v1_5 SHA-256 signature).
/// The public key is read from Mock:SigningPublicKey (Base64 SubjectPublicKeyInfo, or PEM).
/// </summary>
public sealed class JwsVerifier(IConfiguration configuration) : IDisposable
{
    private readonly Lazy<RSA> rsa = new(() => LoadPublicKey(configuration));

    /// <summary>True if the signature verifies; the payload bytes are returned only in that case.</summary>
    public bool TryVerify(string jws, out byte[]? payload)
    {
        payload = null;

        string[] parts = jws.Split('.');
        if (parts.Length != 3
            || !TryDecode(parts[0], out byte[] header)
            || !TryDecode(parts[1], out byte[] body)
            || !TryDecode(parts[2], out byte[] signature)
            || !IsRs256(header))
            return false;

        byte[] signingInput = Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}");
        if (!rsa.Value.VerifyData(signingInput, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            return false;

        payload = body;
        return true;
    }

    /// <summary>Decodes the payload WITHOUT verifying it. Only for labelling results of rejected invoices.</summary>
    public static bool TryReadUnverifiedPayload(string jws, out byte[] payload)
    {
        payload = [];
        string[] parts = jws.Split('.');
        return parts.Length == 3 && TryDecode(parts[1], out payload);
    }

    private static bool TryDecode(string segment, out byte[] bytes)
    {
        try
        {
            bytes = Base64Url.DecodeFromChars(segment);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }

    private static bool IsRs256(byte[] header)
    {
        try
        {
            using var doc = JsonDocument.Parse(header);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("alg", out var alg)
                && alg.ValueKind == JsonValueKind.String
                && alg.GetString() == "RS256";
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static RSA LoadPublicKey(IConfiguration configuration)
    {
        string key = configuration["Mock:SigningPublicKey"]
            ?? throw new InvalidOperationException("Mock:SigningPublicKey is not configured");

        var publicKey = RSA.Create();
        if (key.Contains("-----BEGIN", StringComparison.Ordinal))
            publicKey.ImportFromPem(key);
        else
            publicKey.ImportSubjectPublicKeyInfo(Convert.FromBase64String(key), out _);

        return publicKey;
    }

    public void Dispose()
    {
        if (rsa.IsValueCreated)
            rsa.Value.Dispose();
    }
}
