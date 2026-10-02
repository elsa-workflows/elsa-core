namespace Elsa.Studio.Authentication.OpenIdConnect.Contracts;

/// <summary>
/// Provides access to authentication tokens for API calls.
/// Implementations handle token retrieval and caching based on the hosting model (Server vs WASM).
/// </summary>
public interface ITokenProvider
{
    /// <summary>
    /// Gets an access token for API calls.
    /// For multi-audience scenarios, the implementation may acquire tokens with specific scopes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token value, or null if not available.</returns>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
