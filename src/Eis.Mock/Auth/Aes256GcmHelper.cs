using System.Security.Cryptography;
using System.Text;

namespace Eis.Mock.Auth;

/// <summary>
/// AES-256-GCM in the same layout the middleware's EisSigner.Encrypt writes:
/// Base64( nonce[12] || ciphertext || tag[16] ). The key is the Base64-encoded session key.
/// </summary>
public static class Aes256GcmHelper
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public static string Encrypt(string plainText, string base64Key)
    {
        byte[] key = Convert.FromBase64String(base64Key);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

        byte[] output = new byte[NonceSize + plainBytes.Length + TagSize];
        Span<byte> nonce = output.AsSpan(0, NonceSize);
        Span<byte> cipher = output.AsSpan(NonceSize, plainBytes.Length);
        Span<byte> tag = output.AsSpan(NonceSize + plainBytes.Length, TagSize);

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipher, tag);

        return Convert.ToBase64String(output);
    }

    public static bool TryDecrypt(string base64Cipher, string base64Key, out string plainText)
    {
        plainText = string.Empty;
        try
        {
            byte[] key = Convert.FromBase64String(base64Key);
            byte[] input = Convert.FromBase64String(base64Cipher);
            if (input.Length < NonceSize + TagSize)
                return false;

            int cipherLength = input.Length - NonceSize - TagSize;
            byte[] plainBytes = new byte[cipherLength];

            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(
                input.AsSpan(0, NonceSize),
                input.AsSpan(NonceSize, cipherLength),
                input.AsSpan(NonceSize + cipherLength, TagSize),
                plainBytes);

            plainText = Encoding.UTF8.GetString(plainBytes);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException or ArgumentException)
        {
            // Bad Base64, wrong key size, or authentication tag mismatch
            return false;
        }
    }
}
