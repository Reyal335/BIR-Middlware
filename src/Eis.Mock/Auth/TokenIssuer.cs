using Eis.Mock.Interfaces;
using System;
using System.Security.Cryptography;
using Eis.Mock.Store;

namespace Eis.Mock.Auth;

public class TokenIssuer (SessionStore sessionStore) : ITokenIssuer
{
    public TokenBody IssueTokens()
    {
        string allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        string authToken = RandomNumberGenerator.GetString(allowedChars, 32);

        // Base64 of 32 random bytes: the middleware Base64-decodes it into an AES-256-GCM key,
        // and both sides use the UTF-8 bytes of this string as the HMAC key
        string sessionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        string manilaTimezone = "Asia/Manila";
        TimeZoneInfo manilaZone = TimeZoneInfo.FindSystemTimeZoneById(manilaTimezone);
        DateTime manilaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, manilaZone);
        DateTime expirationTime = manilaTime.AddHours(6);

        // Date formatted to yyyyMMddHHmmss as per BIR regulations
        string tokenExpiry = $"{expirationTime:yyyy-MM-ddTHH:mm:ss}";
        
        // Save auth token and session key to the session store
        sessionStore.AddSession(authToken, new SessionDetails (sessionKey, tokenExpiry));

        return new TokenBody(authToken, sessionKey, tokenExpiry);
    }
}