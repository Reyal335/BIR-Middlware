using System.Text.Json.Serialization;


namespace Eis.Mock.DTO;

public class EisReply
{
    [JsonPropertyName("accreditationId")]
    public string accreditationId { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string userId { get; set; } = string.Empty;

    [JsonPropertyName("authToken")]
    public string authToken { get; set; } = string.Empty;

    [JsonPropertyName("sessionKey")]
    public string sessionKey { get; set; } = string.Empty;

    [JsonPropertyName("tokenExpiry")]
    public string tokenExpiry { get; set; } = string.Empty;
}

public class AuthBody
{
    [JsonPropertyName("data")]
    public string data { get; set; } = string.Empty;

    [JsonPropertyName("forceRefreshToken")]
    public bool forceRefreshToken { get; set; } = false;
}

public class AuthCredentials
{
    [JsonPropertyName("userId")]
    public string userId { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string password { get; set; } = string.Empty;

    [JsonPropertyName("authKey")]
    public string authKey { get; set; } = string.Empty;
}