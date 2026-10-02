namespace Elsa.Studio.Authentication.ElsaIdentity.Contracts;

/// <summary>
/// Defines a provider for retrieving authentication tokens.
/// </summary>
public interface ITokenProvider
{
    /// <summary>
    /// Asynchronously retrieves an access token for authentication purposes.
    /// Uses the default scopes configured for the authentication provider.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the access token as a string or null if not available.</returns>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}