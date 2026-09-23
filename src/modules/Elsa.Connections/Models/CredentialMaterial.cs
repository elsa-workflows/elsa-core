using System.Text.Json.Serialization;

namespace Elsa.Connections.Models;

/// <summary>Token material is serialized into the encrypted Secrets value and must remain short-lived in memory.</summary>
public sealed class CredentialMaterial(string accessToken, string refreshToken, DateTimeOffset accessTokenExpiresAt)
{
    [JsonIgnore]
    public string AccessToken { get; } = accessToken;

    [JsonIgnore]
    public string RefreshToken { get; } = refreshToken;

    [JsonIgnore]
    public DateTimeOffset AccessTokenExpiresAt { get; } = accessTokenExpiresAt;

    public override string ToString() => "CredentialMaterial { Redacted = true }";
}

/// <summary>Access-only credential handed to an authorized consumer; refresh material remains inside lifecycle management.</summary>
public sealed class ConnectionAccessCredential(string accessToken, DateTimeOffset expiresAt)
{
    [JsonIgnore]
    public string AccessToken { get; } = accessToken;

    public DateTimeOffset ExpiresAt { get; } = expiresAt;

    public override string ToString() => "ConnectionAccessCredential { Redacted = true }";
}
