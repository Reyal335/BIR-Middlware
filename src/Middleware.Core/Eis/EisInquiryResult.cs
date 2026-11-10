using System.Text.Json.Serialization;

namespace Middleware.Core.Eis;

/// <summary>Per-invoice result codes returned by the EIS Inquiry Result API.</summary>
public static class EisResultCodes
{
    /// <summary>Invoice accepted. Every other code is a rejection.</summary>
    public const string Accepted = "INV000";
}

/// <summary>Batch statuses reported by the Inquiry Result API.</summary>
public static class EisBatchStatus
{
    /// <summary>Received; invoices are still being validated, so there are no results yet.</summary>
    public const string Received = "RECEIVED";

    /// <summary>Every invoice has a result.</summary>
    public const string Completed = "COMPLETED";
}

/// <summary>Decrypted reply of GET {ResultPath}/{submitId}.</summary>
public class EisInquiryResult
{
    [JsonPropertyName("submitId")]
    public string SubmitId { get; set; } = string.Empty;

    [JsonPropertyName("referenceNo")]
    public string ReferenceNo { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Empty until Status is COMPLETED.</summary>
    [JsonPropertyName("results")]
    public List<EisInvoiceResult> Results { get; set; } = [];
}

public class EisInvoiceResult
{
    /// <summary>1-based position in the batch as the EIS first received it.</summary>
    [JsonPropertyName("position")]
    public int Position { get; set; }

    /// <summary>Null when the EIS could not read the invoice payload.</summary>
    [JsonPropertyName("uniqueId")]
    public string? UniqueId { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>The EIS has no submission with this submitId (404 REQ006).</summary>
public class EisSubmissionNotFoundException(string submitId)
    : Exception($"The EIS has no submission with submitId {submitId}.")
{
    public string SubmitId { get; } = submitId;
}
