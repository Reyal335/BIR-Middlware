using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Eis.Mock.Invoices;

public sealed record ParsedLine(
    int LineNo, decimal Qty, decimal UnitCost, decimal Discount,
    decimal SalesAmt, decimal NetSales, string TaxType);

public sealed record ParsedTaxSummary(
    decimal VatableSales, decimal VatExemptSales, decimal ZeroRatedSales, decimal VatAmount,
    decimal ScPwdDiscount, decimal OtherDiscount, decimal TotalAmountDue);

/// <summary>The fields of a decoded invoice payload that the per-invoice checks use.</summary>
public sealed record ParsedInvoice(
    string UniqueId,
    DateOnly InvoiceDate,
    DateTimeOffset IssuedAt,
    string SellerTin,
    string? SellerVatRegistration,
    IReadOnlyList<ParsedLine> Lines,
    ParsedTaxSummary TaxSummary);

/// <summary>
/// Schema check for the JWS payload. Field names and formats follow the middleware's
/// BirEisInvoicePayload as serialized by EisJson (exact property names, amounts as JSON numbers
/// with at most two decimals). Unknown extra properties are ignored.
/// </summary>
public static partial class InvoicePayloadReader
{
    public static readonly IReadOnlySet<string> TaxTypes =
        new HashSet<string> { "VATABLE", "ZERO_RATED", "VAT_EXEMPT", "VAT_EXEMPT_SCPWD" };

    private const int MaxLines = 1000;

    // EisUniqueIdGenerator: yyyyMMdd + 8-char certification number + 8-digit control number
    [GeneratedRegex(@"^\d{8}[A-Z0-9]{8}\d{8}$")]
    private static partial Regex UniqueIdPattern();

    [GeneratedRegex(@"^\d{9}$")]
    private static partial Regex TinPattern();

    public static bool TryRead(byte[] json, out ParsedInvoice? invoice, out string error)
    {
        invoice = null;
        error = string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(json);
            invoice = Read(doc.RootElement);
            return true;
        }
        catch (JsonException)
        {
            error = "Payload is not valid JSON.";
        }
        catch (SchemaException ex)
        {
            error = ex.Message;
        }
        return false;
    }

    /// <summary>Reads EISUniqueID from a payload without the full schema check (for labelling results).</summary>
    public static string? TryReadUniqueId(byte[] json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("EISUniqueID", out var id)
                && id.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(id.GetString())
                    ? id.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ParsedInvoice Read(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new SchemaException("Payload must be a JSON object.");

        string uniqueId = RequireString(root, "EISUniqueID", "", 24, 24);
        if (!UniqueIdPattern().IsMatch(uniqueId))
            throw new SchemaException("EISUniqueID must be an 8-digit date, 8-character certification number and 8-digit control number.");

        RequireString(root, "DocumentType", "", 1, 30);
        RequireString(root, "TransactionType", "", 1, 30);

        // Header
        JsonElement header = RequireObject(root, "Header", "");
        RequireString(header, "InvoiceNumber", "Header.", 1, 50);
        string date = RequireString(header, "InvoiceDate", "Header.", 10, 10);
        string time = RequireString(header, "InvoiceTime", "Header.", 14, 14);
        RequireString(header, "Currency", "Header.", 3, 3);

        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly invoiceDate))
            throw new SchemaException("Header.InvoiceDate must be yyyy-MM-dd.");

        if (!DateTimeOffset.TryParseExact($"{date}T{time}", "yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTimeOffset issuedAt))
            throw new SchemaException("Header.InvoiceTime must be HH:mm:ss+hh:mm.");

        // Seller
        JsonElement seller = RequireObject(root, "Seller", "");
        string sellerTin = RequireString(seller, "Tin", "Seller.", 9, 9);
        if (!TinPattern().IsMatch(sellerTin))
            throw new SchemaException("Seller.Tin must be 9 digits.");
        RequireString(seller, "RegisteredName", "Seller.", 1, 200);
        string? vatRegistration = OptionalString(seller, "VatRegistration", "Seller.", 20);

        // Buyer (Tin is null for walk-in buyers)
        JsonElement buyer = RequireObject(root, "Buyer", "");
        string? buyerTin = OptionalString(buyer, "Tin", "Buyer.", 9);
        if (buyerTin is not null && !TinPattern().IsMatch(buyerTin))
            throw new SchemaException("Buyer.Tin must be 9 digits or null.");
        RequireString(buyer, "RegisteredName", "Buyer.", 1, 200);

        // Items
        JsonElement items = Require(root, "ItemList", "", JsonValueKind.Array);
        int count = items.GetArrayLength();
        if (count is < 1 or > MaxLines)
            throw new SchemaException($"ItemList must have 1-{MaxLines} lines.");

        var lines = new List<ParsedLine>(count);
        int index = 0;
        foreach (JsonElement item in items.EnumerateArray())
        {
            string path = $"ItemList[{index++}].";
            if (item.ValueKind != JsonValueKind.Object)
                throw new SchemaException($"{path.TrimEnd('.')} must be an object.");

            int lineNo = RequireInt(item, "LineNo", path);
            RequireString(item, "Nm", path, 1, 200);
            decimal qty = RequireAmount(item, "Qty", path);
            if (qty <= 0)
                throw new SchemaException($"{path}Qty must be greater than 0.");

            string taxType = RequireString(item, "TaxType", path, 1, 20);
            if (!TaxTypes.Contains(taxType))
                throw new SchemaException($"{path}TaxType must be one of {string.Join(", ", TaxTypes)}.");

            lines.Add(new ParsedLine(
                lineNo,
                qty,
                RequireAmount(item, "UnitCost", path, nonNegative: true),
                RequireAmount(item, "Discount", path, nonNegative: true),
                RequireAmount(item, "SalesAmt", path, nonNegative: true),
                RequireAmount(item, "NetSales", path),
                taxType));
        }

        // Tax summary
        JsonElement tax = RequireObject(root, "TaxSummary", "");
        const string tp = "TaxSummary.";
        var summary = new ParsedTaxSummary(
            RequireAmount(tax, "VATableSales", tp, nonNegative: true),
            RequireAmount(tax, "VATExemptSales", tp, nonNegative: true),
            RequireAmount(tax, "ZeroRatedSales", tp, nonNegative: true),
            RequireAmount(tax, "VATAmount", tp, nonNegative: true),
            RequireAmount(tax, "SCPWDDiscount", tp, nonNegative: true),
            RequireAmount(tax, "OtherDiscount", tp, nonNegative: true),
            RequireAmount(tax, "TotalAmountDue", tp));

        return new ParsedInvoice(uniqueId, invoiceDate, issuedAt, sellerTin, vatRegistration, lines, summary);
    }

    private static JsonElement Require(JsonElement parent, string name, string path, JsonValueKind kind)
    {
        if (!parent.TryGetProperty(name, out JsonElement value))
            throw new SchemaException($"{path}{name} is required.");
        if (value.ValueKind != kind)
            throw new SchemaException($"{path}{name} must be {Describe(kind)}.");
        return value;
    }

    private static JsonElement RequireObject(JsonElement parent, string name, string path) =>
        Require(parent, name, path, JsonValueKind.Object);

    private static string RequireString(JsonElement parent, string name, string path, int minLength, int maxLength)
    {
        string value = Require(parent, name, path, JsonValueKind.String).GetString()!;
        CheckLength(value, $"{path}{name}", minLength, maxLength);
        return value;
    }

    private static string? OptionalString(JsonElement parent, string name, string path, int maxLength)
    {
        if (!parent.TryGetProperty(name, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
            return null;
        if (value.ValueKind != JsonValueKind.String)
            throw new SchemaException($"{path}{name} must be a string or null.");

        string text = value.GetString()!;
        CheckLength(text, $"{path}{name}", 0, maxLength);
        return text.Length == 0 ? null : text;
    }

    private static void CheckLength(string value, string field, int minLength, int maxLength)
    {
        if (value.Length < minLength || value.Length > maxLength)
            throw new SchemaException(minLength == maxLength
                ? $"{field} must be exactly {maxLength} characters."
                : $"{field} must be {minLength}-{maxLength} characters.");
    }

    private static int RequireInt(JsonElement parent, string name, string path)
    {
        JsonElement value = Require(parent, name, path, JsonValueKind.Number);
        if (!value.TryGetInt32(out int number) || number < 1)
            throw new SchemaException($"{path}{name} must be a positive integer.");
        return number;
    }

    private static decimal RequireAmount(JsonElement parent, string name, string path, bool nonNegative = false)
    {
        JsonElement value = Require(parent, name, path, JsonValueKind.Number);
        if (!value.TryGetDecimal(out decimal amount) || decimal.Round(amount, 2) != amount)
            throw new SchemaException($"{path}{name} must be a number with at most two decimal places.");
        if (nonNegative && amount < 0)
            throw new SchemaException($"{path}{name} must not be negative.");
        return amount;
    }

    private static string Describe(JsonValueKind kind) => kind switch
    {
        JsonValueKind.Object => "an object",
        JsonValueKind.Array => "an array",
        JsonValueKind.String => "a string",
        JsonValueKind.Number => "a number",
        _ => kind.ToString(),
    };

    private sealed class SchemaException(string message) : Exception(message);
}
