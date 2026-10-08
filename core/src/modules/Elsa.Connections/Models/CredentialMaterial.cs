using System.Text.Json;
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
[JsonConverter(typeof(ConnectionAccessCredentialJsonConverter))]
public sealed class ConnectionAccessCredential
{
    private readonly DateTimeOffset? _expiresAt;

    [JsonIgnore]
    public string AccessToken { get; }

    [JsonIgnore]
    public string? ApiKey { get; }

    public ConnectionCredentialKind Kind { get; }

    /// <summary>Gets the OAuth expiry. API-key credentials are explicitly non-expiring and have no expiry value.</summary>
    public DateTimeOffset ExpiresAt => _expiresAt ?? throw new InvalidOperationException("API-key credentials do not have an expiry.");

    public ConnectionAccessCredential(string accessToken, DateTimeOffset expiresAt)
        : this(ConnectionCredentialKind.OAuth, accessToken, expiresAt)
    {
    }

    public ConnectionAccessCredential(ConnectionCredentialKind kind, string value, DateTimeOffset? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Credential value is required.", nameof(value));
        }

        if (kind == ConnectionCredentialKind.OAuth && !expiresAt.HasValue)
        {
            throw new ArgumentException("OAuth credentials require an expiry.", nameof(expiresAt));
        }

        if (kind == ConnectionCredentialKind.ApiKey && expiresAt.HasValue)
        {
            throw new ArgumentException("API-key credentials do not have an expiry.", nameof(expiresAt));
        }

        Kind = kind;
        AccessToken = value;
        ApiKey = kind == ConnectionCredentialKind.ApiKey ? value : null;
        _expiresAt = expiresAt;
    }

    public override string ToString() => "ConnectionAccessCredential { Redacted = true }";
}

internal sealed class ConnectionAccessCredentialJsonConverter : JsonConverter<ConnectionAccessCredential>
{
    public override ConnectionAccessCredential Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new JsonException("Connection access credentials are write-only.");

    public override void Write(Utf8JsonWriter writer, ConnectionAccessCredential value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber(GetName(nameof(ConnectionAccessCredential.Kind)), (int)value.Kind);

        if (value.Kind == ConnectionCredentialKind.OAuth)
        {
            writer.WriteString(GetName(nameof(ConnectionAccessCredential.ExpiresAt)), value.ExpiresAt);
        }

        writer.WriteEndObject();

        string GetName(string name) => options.PropertyNamingPolicy?.ConvertName(name) ?? name;
    }
}
