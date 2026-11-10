using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Eis.Mock.Invoices;
using Eis.Mock.Store;

namespace Eis.Mock.Auth;

/// <summary>Why a request failed authentication: the HTTP status and the request-level error.</summary>
public sealed record RequestFailure(int StatusCode, ErrorResponse Error);

/// <summary>
/// Checks shared by every session-authenticated EIS endpoint (issuance, inquiry), in order:
/// 1. bearer token present, 2. token known and not expired, 3. datetime header within 10 minutes,
/// 4. HMAC-SHA256 signature keyed with the session key.
/// </summary>
public class RequestAuthenticator(SessionStore sessionStore)
{
    public const string DatetimeFormat = "yyyyMMddHHmmss";          // HmacSigningHandler
    private const string TokenExpiryFormat = "yyyy-MM-ddTHH:mm:ss"; // TokenIssuer
    private static readonly TimeSpan DatetimeWindow = TimeSpan.FromMinutes(10);

    /// <summary>Returns null and sets the session key and request time when all checks pass.</summary>
    public RequestFailure? Authenticate(
        string? authorization, string? signature, string? datetime, string method, string path,
        out string sessionKey, out DateTime requestTime)
    {
        sessionKey = string.Empty;
        requestTime = default;

        // 1. Bearer token present
        string? token = ReadBearerToken(authorization);
        if (token is null)
            return Fail(InvoiceErrorCodes.MissingToken, "Missing bearer token.");

        // 2. Token known and not expired
        if (!sessionStore.TryGetSession(token, out var session) || IsExpired(session))
            return Fail(InvoiceErrorCodes.InvalidToken, "Token invalid or expired.");

        // 3. Request datetime within 10 minutes of the mock's clock
        if (!DateTime.TryParseExact(datetime, DatetimeFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out requestTime)
            || (requestTime - ManilaClock.Now()).Duration() > DatetimeWindow)
            return Fail(InvoiceErrorCodes.DatetimeOutOfWindow, "Request datetime outside allowed window.");

        // 4. HMAC-SHA256 signature, keyed with the session key
        if (!SignatureMatches(signature, datetime!, method, path, session.sessionKey))
            return Fail(InvoiceErrorCodes.InvalidSignature, "Invalid request signature.");

        sessionKey = session.sessionKey;
        return null;
    }

    private static RequestFailure Fail(string code, string message) =>
        new(StatusCodes.Status401Unauthorized, new ErrorResponse(code, message));

    private static string? ReadBearerToken(string? authorization)
    {
        const string prefix = "Bearer ";
        if (authorization is null || !authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        string token = authorization[prefix.Length..].Trim();
        return token.Length == 0 ? null : token;
    }

    private static bool IsExpired(SessionDetails session)
    {
        // An unreadable expiry is treated as expired
        if (!DateTime.TryParseExact(session.tokenExpirty, TokenExpiryFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime expiresAt))
            return true;

        return expiresAt <= ManilaClock.Now();
    }

    /// <summary>
    /// Same string-to-sign as the middleware's HmacSigningHandler:
    /// "{datetime}/{METHOD}/{AbsolutePath}", HMAC-SHA256 keyed with the UTF-8 session key, Base64.
    /// </summary>
    private static bool SignatureMatches(string? signature, string datetime, string method, string path, string sessionKey)
    {
        if (string.IsNullOrWhiteSpace(signature))
            return false;

        byte[] provided;
        try
        {
            provided = Convert.FromBase64String(signature.Trim());
        }
        catch (FormatException)
        {
            return false;
        }

        string message = $"{datetime}/{method}/{path}";
        byte[] expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(sessionKey), Encoding.UTF8.GetBytes(message));

        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }
}
