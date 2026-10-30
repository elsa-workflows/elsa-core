namespace Elsa.OrchardCore.Client.Models;

/// <summary>
/// Represents a security token used for authentication, including access token,
/// token type, and expiration information.
/// </summary>
public record SecurityToken(string AccessToken, string TokenType, int ExpiresIn);