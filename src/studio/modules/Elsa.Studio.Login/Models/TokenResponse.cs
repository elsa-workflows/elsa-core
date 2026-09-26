using System.Text.Json.Serialization;

namespace Elsa.Studio.Login.Models;

/// <summary>
/// Represents a response containing token.
/// </summary>
public sealed class TokenResponse
{
    [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    [JsonPropertyName("id_token")] public string? IdToken { get; set; }
    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
}