using System.Text.Json.Serialization;

namespace Middleware.Core.Eis;

public class EisAuthReply
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
}

public class EisAuthCredentials
{
    [JsonPropertyName("accreditationId")]
    public string AccreditationId { get; set; } = string.Empty;
    
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("authToken")]
    public string AuthToken { get; set; } = string.Empty;

    [JsonPropertyName("sessionKey")]
    public string SessionKey { get; set; } = string.Empty;

    [JsonPropertyName("tokenExpiry")]
    public string TokenExpiry { get; set; } = string.Empty;

}

