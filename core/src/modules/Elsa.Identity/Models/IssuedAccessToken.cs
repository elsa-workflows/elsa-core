namespace Elsa.Identity.Models;

/// <summary>
/// Represents an issued Elsa token.
/// </summary>
/// <param name="Token">The serialized token.</param>
/// <param name="ExpiresAt">The token expiration time.</param>
public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAt);
