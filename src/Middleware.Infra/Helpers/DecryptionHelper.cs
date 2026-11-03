using System.Security.Cryptography;
using System.Text;

namespace Middleware.Infra.Helpers;

public static class Aes256CbcHelper
{
    public static string Decrypt(string base64Cipher, string key)
    {
        var (keyBytes, iv) = GetKeyAndIv(key);

        using var aes = Aes.Create();
        aes.Key = keyBytes;

        var plain = aes.DecryptCbc(Convert.FromBase64String(base64Cipher), iv, PaddingMode.PKCS7);
        return Encoding.UTF8.GetString(plain);
    }

    private static (byte[] Key, byte[] Iv) GetKeyAndIv(string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        if (keyBytes.Length != 32)
            throw new ArgumentException("AES key must be exactly 32 bytes (32 ASCII characters).");

        return (keyBytes, keyBytes[..16]);   // first 16 chars = first 16 bytes for ASCII keys
    }
}