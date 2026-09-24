using System.Text.Json.Serialization;

namespace Elsa.Connections.Models;

public enum ConnectionCredentialKind
{
    OAuth = 0,
    ApiKey = 1
}

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

/// <summary>Credential handed to an authorized consumer. Refresh material remains inside lifecycle management.</summary>
public sealed class ConnectionAccessCredential
{
    [JsonIgnore]
    public string AccessToken { get; }

    public ConnectionCredentialKind Kind { get; }

    public DateTimeOffset? ExpiresAt { get; }

    public ConnectionAccessCredential(string accessToken, DateTimeOffset expiresAt)
        : this(ConnectionCredentialKind.OAuth, accessToken, expiresAt)
    {
    }

    public ConnectionAccessCredential(ConnectionCredentialKind kind, string value, DateTimeOffset? expiresAt)
    {
        Kind = kind;
        AccessToken = value;
        ExpiresAt = expiresAt;
    }

    public override string ToString() => "ConnectionAccessCredential { Redacted = true }";
}
