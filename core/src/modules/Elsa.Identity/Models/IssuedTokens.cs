namespace Elsa.Identity.Models;

/// <summary>
/// Represents issued tokens.
/// </summary>
/// <param name="AccessToken">The access token.</param>
/// <param name="RefreshToken">The refresh token.</param>
public record IssuedTokens(string AccessToken, string RefreshToken);