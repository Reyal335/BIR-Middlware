using System.Text.Json.Serialization;

namespace Eis.Mock.Dto;

/// <summary>Inquiry Result reply; returned encrypted in the response's data field.</summary>
public class InquiryReply
{
    [JsonPropertyName("submitId")]
    public string submitId { get; set; } = string.Empty;

    [JsonPropertyName("referenceNo")]
    public string referenceNo { get; set; } = string.Empty;

    /// <summary>RECEIVED while invoices are still being validated, then COMPLETED.</summary>
    [JsonPropertyName("status")]
    public string status { get; set; } = string.Empty;

    /// <summary>Empty until the batch is COMPLETED; ordered by position.</summary>
    [JsonPropertyName("results")]
    public List<InquiryInvoiceResult> results { get; set; } = [];
}

public class InquiryInvoiceResult
{
    /// <summary>1-based position of the JWS in the submitted batch.</summary>
    [JsonPropertyName("position")]
    public int position { get; set; }

    /// <summary>EIS unique ID, or null when the payload could not be read.</summary>
    [JsonPropertyName("uniqueId")]
    public string? uniqueId { get; set; }

    [JsonPropertyName("code")]
    public string code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string message { get; set; } = string.Empty;
}
