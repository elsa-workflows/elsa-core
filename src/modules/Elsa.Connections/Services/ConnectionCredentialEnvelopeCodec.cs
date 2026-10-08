using System.Text.Json;
using Elsa.Connections.Models;

namespace Elsa.Connections.Services;

// Shared only with the lockstep Socket listener. This codec grants no credential-use authority.
internal static class ConnectionCredentialEnvelopeCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static string Serialize(CredentialMaterial material) => JsonSerializer.Serialize(
        new ConnectionCredentialEnvelope(ConnectionCredentialKind.OAuth, material.AccessToken, material.RefreshToken, material.AccessTokenExpiresAt), JsonOptions);

    internal static string SerializeApiKey(string apiKey) => JsonSerializer.Serialize(
        new ConnectionCredentialEnvelope(ConnectionCredentialKind.ApiKey, apiKey, null, null), JsonOptions);

    internal static ConnectionCredentialEnvelope? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }
        try
        {
            return JsonSerializer.Deserialize<ConnectionCredentialEnvelope>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static bool IsValidEnvelope(ConnectionCredentialEnvelope? envelope)
    {
        if (envelope == null || string.IsNullOrWhiteSpace(envelope.AccessToken))
        {
            return false;
        }
        return envelope.Kind switch
        {
            null or ConnectionCredentialKind.OAuth => !string.IsNullOrWhiteSpace(envelope.RefreshToken) && envelope.AccessTokenExpiresAt.HasValue,
            ConnectionCredentialKind.ApiKey => envelope.RefreshToken is null && !envelope.AccessTokenExpiresAt.HasValue,
            _ => false
        };
    }
}

internal sealed record ConnectionCredentialEnvelope(ConnectionCredentialKind? Kind, string? AccessToken, string? RefreshToken, DateTimeOffset? AccessTokenExpiresAt)
{
    public override string ToString() => "ConnectionCredentialEnvelope { Redacted = true }";
}
