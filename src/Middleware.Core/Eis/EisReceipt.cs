using System.Text.Json.Serialization;

namespace Middleware.Core.Eis;

/// <summary>
/// Batch receipt the EIS returns (encrypted with the session key) when it accepts a submission.
/// Per-invoice results are fetched separately from the Inquiry Result API.
/// </summary>
public class EisReceipt
{
    [JsonPropertyName("submitId")]
    public string SubmitId { get; set; } = string.Empty;

    [JsonPropertyName("referenceNo")]
    public string ReferenceNo { get; set; } = string.Empty;

    /// <summary>yyyy-MM-ddTHH:mm:ss, Manila time (EIS clock).</summary>
    [JsonPropertyName("receivedAt")]
    public string ReceivedAt { get; set; } = string.Empty;

    [JsonPropertyName("invoiceCount")]
    public int InvoiceCount { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}
