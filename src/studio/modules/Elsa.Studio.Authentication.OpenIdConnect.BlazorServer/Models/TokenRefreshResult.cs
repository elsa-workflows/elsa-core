namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Models;

/// <summary>
/// Result of a token refresh operation.
/// </summary>
public sealed class TokenRefreshResult
{
    /// <summary>
    /// Whether the refresh was successful.
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// The new access token.
    /// </summary>
    public string? AccessToken { get; init; }
    
    /// <summary>
    /// The new refresh token (if rotated by the IdP).
    /// </summary>
    public string? RefreshToken { get; init; }
    
    /// <summary>
    /// When the access token expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }
    
    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static TokenRefreshResult Failed() => new() { Success = false };
}