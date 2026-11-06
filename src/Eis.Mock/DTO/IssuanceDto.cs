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