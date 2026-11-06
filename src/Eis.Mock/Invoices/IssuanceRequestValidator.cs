using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Eis.Mock.Auth;
using Eis.Mock.Dto;
using Eis.Mock.Interfaces;
using Eis.Mock.Store;

namespace Eis.Mock.Invoices;

/// <summary>A request that passed every transport check.</summary>
public sealed record ValidatedIssuance(
    string SubmitId,
    string SessionKey,
    DateTime TransmittedAt,
    IReadOnlyList<string> Jws);

/// <summary>
/// Transport-level checks for POST /api/v2/invoices, run in order; the first failure wins.
/// Individual invoices are NOT validated here (see InvoiceValidationService).
/// </summary>
public partial class IssuanceRequestValidator(SessionStore sessionStore, SubmissionStore submissionStore)
{
    public const int MaxInvoices = 100;
    private const string DatetimeFormat = "yyyyMMddHHmmss";        // HmacSigningHandler
    private const string TokenExpiryFormat = "yyyy-MM-ddTHH:mm:ss"; // TokenIssuer
    private static readonly TimeSpan DatetimeWindow = TimeSpan.FromMinutes(10);

    // SubmitIdGenerator: {CertificationNumber}-{yyyyMMddHHmmss}-{4 hex}
    [GeneratedRegex(@"^[A-Z0-9]{8}-(?<ts>\d{14})-[0-9A-F]{4}$")]
    private static partial Regex SubmitIdPattern();

    /// <summary>Returns null and sets <paramref name="validated"/> when all checks pass; otherwise the error result.</summary>
    public IssuanceResult? Validate(IssuanceRequest request, out ValidatedIssuance? validated)
    {
        validated = null;

        // Best effort, only so error bodies can echo the submission ID
        string? submitId = TryReadSubmitId(request.Body);

        // 1. Bearer token present
        string? token = ReadBearerToken(request.Authorization);
        if (token is null)
            return Fail(401, InvoiceErrorCodes.MissingToken, "Missing bearer token.", submitId);

        // 2. Token known and not expired
        if (!sessionStore.TryGetSession(token, out var session) || IsExpired(session))
            return Fail(401, InvoiceErrorCodes.InvalidToken, "Token invalid or expired.", submitId);

        // 3. Request datetime within 10 minutes of the mock's clock
        if (!DateTime.TryParseExact(request.Datetime, DatetimeFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime transmittedAt)
            || (transmittedAt - ManilaClock.Now()).Duration() > DatetimeWindow)
            return Fail(401, InvoiceErrorCodes.DatetimeOutOfWindow, "Request datetime outside allowed window.", submitId);

        // 4. HMAC-SHA256 signature, keyed with the session key
        if (!SignatureMatches(request, session.sessionKey))
            return Fail(401, InvoiceErrorCodes.InvalidSignature, "Invalid request signature.", submitId);

        // 5. Body parses and has both fields
        IssuanceDto? body = ParseBody(request.Body);
        if (body is null || string.IsNullOrWhiteSpace(body.submitId) || string.IsNullOrWhiteSpace(body.data))
            return Fail(400, InvoiceErrorCodes.MalformedBody, "Malformed request body.", submitId);

        // 6. Submission ID format
        if (!IsValidSubmitId(body.submitId))
            return Fail(400, InvoiceErrorCodes.InvalidSubmitId, "Invalid submitId format.", body.submitId);

        // 7. Not already submitted
        if (submissionStore.Contains(body.submitId))
            return Fail(409, InvoiceErrorCodes.DuplicateSubmitId, "Duplicate submitId.", body.submitId);

        // 8. Decrypt with the session key
        if (!Aes256GcmHelper.TryDecrypt(body.data, session.sessionKey, out string plainText))
            return Fail(400, InvoiceErrorCodes.DecryptionFailed, "Unable to decrypt data.", body.submitId);

        // 9. 1-100 JWS strings
        List<string>? jws = ParseInvoices(plainText);
        if (jws is null || jws.Count is < 1 or > MaxInvoices)
            return Fail(400, InvoiceErrorCodes.InvalidInvoiceCount, $"Invoice count must be 1-{MaxInvoices}.", body.submitId);

        validated = new ValidatedIssuance(body.submitId, session.sessionKey, transmittedAt, jws);
        return null;
    }

    public static IssuanceResult Fail(int statusCode, string code, string message, string? submitId) =>
        new(statusCode, null, new ErrorResponse(code, message), submitId);

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
    private static bool SignatureMatches(IssuanceRequest request, string sessionKey)
    {
        if (string.IsNullOrWhiteSpace(request.Signature))
            return false;

        byte[] provided;
        try
        {
            provided = Convert.FromBase64String(request.Signature.Trim());
        }
        catch (FormatException)
        {
            return false;
        }

        string message = $"{request.Datetime}/{request.Method}/{request.Path}";
        byte[] expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(sessionKey), Encoding.UTF8.GetBytes(message));

        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }

    private static IssuanceDto? ParseBody(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<IssuanceDto>(body);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TryReadSubmitId(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("submitId", out var id)
                && id.ValueKind == JsonValueKind.String
                    ? id.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsValidSubmitId(string submitId)
    {
        var match = SubmitIdPattern().Match(submitId);
        return match.Success
            && DateTime.TryParseExact(match.Groups["ts"].Value, DatetimeFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _);
    }

    private static List<string>? ParseInvoices(string plainText)
    {
        try
        {
            // Non-string array entries throw here and are reported as an invalid invoice list
            var payload = JsonSerializer.Deserialize<IssuancePayload>(plainText);
            var invoices = payload?.invoices;
            return invoices is not null && invoices.TrueForAll(j => !string.IsNullOrWhiteSpace(j)) ? invoices : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
