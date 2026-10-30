namespace Elsa.Studio.ExternalAuthentication.BlazorWasm.Models;

/// <summary>Safe client-side representation of a broker-issued token response.</summary>
public sealed record ExternalAuthenticationTokenSet(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt,
    DateTimeOffset ExternalSessionExpiresAt);
