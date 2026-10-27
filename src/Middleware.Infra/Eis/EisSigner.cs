
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

    public string Encrypt(string JWS)
    {
        return "sdfdsf";
    }

    public void Dispose() => _rsa.Dispose();
}
