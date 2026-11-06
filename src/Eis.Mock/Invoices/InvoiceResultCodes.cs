namespace Eis.Mock.Invoices;

/// <summary>Per-invoice result codes, assigned after the batch receipt is returned.</summary>
public static class InvoiceResultCodes
{
    /// <summary>Invoice accepted.</summary>
    public const string Accepted = "INV000";

    /// <summary>JWS signature does not verify against the middleware's public key.</summary>
    public const string SignatureInvalid = "INV001";

    /// <summary>Required field missing, wrong type or wrong length.</summary>
    public const string SchemaInvalid = "INV002";

    /// <summary>EIS unique ID was already accepted in an earlier submission.</summary>
    public const string DuplicateUniqueId = "INV003";

    /// <summary>Seller TIN is not registered.</summary>
    public const string UnregisteredTin = "INV004";

    /// <summary>Issuance date does not match the unique ID, or is after the transmission.</summary>
    public const string InvalidDatetime = "INV005";

    /// <summary>Line amounts, totals or VAT do not recompute.</summary>
    public const string TotalsMismatch = "INV006";
}
