namespace Middleware.Core.interfaces;  

public interface IEisSigner
{
    string Sign(string json);
    string Encrypt(string data, string sessionKey);
    string Decrypt(string data, string sessionKey);
}