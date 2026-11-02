using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Eis.Mock.Auth;

public class RSAHelper 
{
    private readonly IConfiguration _configuration;
    public RSAHelper (IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public T DecryptBase64Rsa<T>(string base64EncryptData, string privateKeyXmlOrPem)
    {
        byte[] encryptedBytes = Convert.FromBase64String(base64EncryptData);

        byte[] decryptedBytes;
        using (RSA rsa = RSA.Create())
        {
            string privateKey = _configuration.GetValue<String>("Mock:EisPrivateKey");
            if (privateKey is null) 
                throw new ArgumentNullException("Mock Eis Private key cannot be null");
            rsa.ImportFromPem(privateKey);

            decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);
        }

        string jsonString = Encoding.UTF8.GetString(decryptedBytes);

        return JsonSerializer.Deserialize<T>(jsonString);
    }
}