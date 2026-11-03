using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Eis.Mock.Auth;

public static class RSAHelper
{
    public static T DecryptBase64Rsa<T>(string base64EncryptData, string privateKeyPem)
    {
        byte[] encryptedBytes = Convert.FromBase64String(base64EncryptData);

        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);

        return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(decryptedBytes));
    }
}
