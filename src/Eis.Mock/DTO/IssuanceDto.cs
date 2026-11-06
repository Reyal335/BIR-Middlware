using System.Text.Json;
using System.Text.Json.Serialization;

namespace Eis.Mock.Dto;

public class IssuanceDto
{
    [JsonPropertyName("submitId")]
    public string submitId { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public string data { get; set; } = string.Empty;
}

/// <summary>Decrypted contents of IssuanceDto.data, as the middleware's HttpEisClient serializes it.</summary>
public class IssuancePayload
{
    [JsonPropertyName("invoices")]
    public List<string>? invoices { get; set; }
}

/// <summary>Batch receipt; returned encrypted in the response's data field.</summary>
public class IssuanceReceipt
{
    [JsonPropertyName("submitId")]
    public string submitId { get; set; } = string.Empty;

    [JsonPropertyName("referenceNo")]
    public string referenceNo { get; set; } = string.Empty;

    /// <summary>yyyy-MM-ddTHH:mm:ss, Manila time (same format as tokenExpiry).</summary>
    [JsonPropertyName("receivedAt")]
    public string receivedAt { get; set; } = string.Empty;

    [JsonPropertyName("invoiceCount")]
    public int invoiceCount { get; set; }

    [JsonPropertyName("status")]
    public string status { get; set; } = string.Empty;
}
