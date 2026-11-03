using System.Security.Cryptography;
using System.Text;

namespace Eis.Mock.Auth;

public static class Aes256CbcHelper
{
    public static string Encrypt(string plainText, string Key)
    {
        var (keyBytes, iv) = GetKeyAndIv(Key);

        using var aes = Aes.Create();
        aes.Key = keyBytes;

        var cipher = aes.EncryptCbc(Encoding.UTF8.GetBytes(plainText), iv, PaddingMode.PKCS7);
        return Convert.ToBase64String(cipher);
    }

    private static (byte[] Key, byte[] Iv) GetKeyAndIv(string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        if (keyBytes.Length != 32)
            throw new ArgumentException("AES key must be exactly 32 bytes (32 ASCII characters).");

        return (keyBytes, keyBytes[..16]);   // first 16 chars = first 16 bytes for ASCII keys
    }
}