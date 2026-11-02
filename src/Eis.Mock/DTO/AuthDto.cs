using System.Text.Json.Serialization;

namespace Eis.Mock.DTO;

public class AuthDto
{
    [JsonPropertyName("userId")]
    string userId = string.Empty;

    [JsonPropertyName("passwrod")]
    string password = string.Empty;

    [JsonPropertyName("authKey")]
    string authKey = string.Empty;
}