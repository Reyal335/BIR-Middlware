namespace Eis.Mock.Invoices;

public static class InvoiceErrorCodes
{
    // Authentication (HTTP 401)
 
    /// <summary>401 — Missing bearer token.</summary>
    public const string MissingToken = "AUT001";
 
    /// <summary>401 — Token invalid or expired.</summary>
    public const string InvalidToken = "AUT002";
 
    /// <summary>401 — Request datetime outside allowed window.</summary>
    public const string DatetimeOutOfWindow = "AUT003";
 
    /// <summary>401 — Invalid request signature.</summary>
    public const string InvalidSignature = "AUT004";
 
    // Request (HTTP 400 / 409)
 
    /// <summary>400 — Malformed request body.</summary>
    public const string MalformedBody = "REQ001";
 
    /// <summary>400 — Invalid submitId format.</summary>
    public const string InvalidSubmitId = "REQ002";
 
    /// <summary>409 — Duplicate submitId.</summary>
    public const string DuplicateSubmitId = "REQ003";
 
    /// <summary>400 — Unable to decrypt data.</summary>
    public const string DecryptionFailed = "REQ004";
 
    /// <summary>400 — Invoice count must be 1–100.</summary>
    public const string InvalidInvoiceCount = "REQ005";

    /// <summary>404 — No submission with this submitId (inquiry).</summary>
    public const string SubmitIdNotFound = "REQ006";
 
    // System (HTTP 500)
 
    /// <summary>500 — Internal error.</summary>
    public const string InternalError = "SYS001";
}