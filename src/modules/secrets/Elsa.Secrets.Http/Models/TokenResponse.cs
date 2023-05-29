using Newtonsoft.Json;

namespace Elsa.Secrets.Http.Models;

public class TokenResponse {
    [JsonProperty("token_type")]
    public string? TokenType { get; set; }
    [JsonProperty("access_token")]
    public string? AccessToken { get; set; }
    [JsonProperty("refresh_token")]
    public string? RefreshToken { get; set; }
    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }
    [JsonProperty("error")]
    public string? Error { get; set; }
}