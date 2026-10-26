namespace Middleware.Core.interfaces;  

public interface IEisSigner
{
    string Sign(string json);
    string Encrypt(string jws);
}