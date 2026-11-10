
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Middleware.Core.interfaces;

namespace Middleware.Infra.Eis;

/// <summary>
/// Produces a compact JWS (RS256) over the invoice JSON: base64url(header).base64url(payload).base64url(signature).
/// The payload is signed exactly as given — no claims are added — so the signature covers the invoice bytes verbatim.
/// </summary>
public sealed class EisSigner : IEisSigner, IDisposable
{
    private readonly RSA _rsa = RSA.Create();
    private readonly string _header;

    private const int NonceSize = 12;
    private const int TagSize = 16;

    public EisSigner(IOptions<EisOptions> options)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.SigningKey))
            throw new InvalidOperationException("Eis:SigningKeyPath is not configured.");

        // Accepts PEM-encoded PKCS#8 ("BEGIN PRIVATE KEY") or PKCS#1 ("BEGIN RSA PRIVATE KEY")
        _rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(opts.SigningKey), out _);

        var header = new Dictionary<string, string> { ["alg"] = "RS256" };
        if (!string.IsNullOrWhiteSpace(opts.SigningKeyId))
            header["kid"] = opts.SigningKeyId;

        _header = Base64UrlEncoder.Encode(JsonSerializer.SerializeToUtf8Bytes(header));
    }

    public string Sign(string invoiceJson)
    {
        ArgumentException.ThrowIfNullOrEmpty(invoiceJson);

        var payload = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(invoiceJson));
        var signingInput = Encoding.ASCII.GetBytes($"{_header}.{payload}");
        var signature = _rsa.SignData(signingInput, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return $"{_header}.{payload}.{Base64UrlEncoder.Encode(signature)}";
    }

    public string Encrypt(string data, string sessionKeyString)
    {

        byte[] sessionKey = Convert.FromBase64String(sessionKeyString);

        // data is plain JSON text, not Base64
        byte[] plainBytes = Encoding.UTF8.GetBytes(data);

        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] cipherBytes = new byte[plainBytes.Length];
        byte[] tag = new byte[TagSize];

        using var aes = new AesGcm(sessionKey, tag.Length);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // nonce || ciphertext || tag
        byte[] output = new byte[nonce.Length + cipherBytes.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, output, 0, nonce.Length);
        Buffer.BlockCopy(cipherBytes, 0, output, nonce.Length, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, output, nonce.Length + cipherBytes.Length, tag.Length);

        return Convert.ToBase64String(output);
    }

    /// <summary>Reverses Encrypt: Base64( nonce || ciphertext || tag ) → UTF-8 text.</summary>
    public string Decrypt(string data, string sessionKeyString)
    {
        byte[] sessionKey = Convert.FromBase64String(sessionKeyString);
        byte[] input = Convert.FromBase64String(data);

        if (input.Length < NonceSize + TagSize)
            throw new CryptographicException("Encrypted data is too short.");

        int cipherLength = input.Length - NonceSize - TagSize;
        byte[] plainBytes = new byte[cipherLength];

        using var aes = new AesGcm(sessionKey, TagSize);
        aes.Decrypt(
            input.AsSpan(0, NonceSize),
            input.AsSpan(NonceSize, cipherLength),
            input.AsSpan(NonceSize + cipherLength, TagSize),
            plainBytes);   // throws if the tag doesn't match (wrong key or tampered data)

        return Encoding.UTF8.GetString(plainBytes);
    }

    public void Dispose() => _rsa.Dispose();
}
