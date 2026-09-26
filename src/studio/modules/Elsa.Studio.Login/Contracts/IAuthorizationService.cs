namespace Elsa.Studio.Login.Contracts;

/// <summary>
/// Provides a service to acquire an access_token from an authorization server
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Redirects the user agent to the authorization server for authentication
    /// </summary>
    Task RedirectToAuthorizationServer();

    /// <summary>
    /// Trades a authorization code received from the authorization server for an access_token
    /// </summary>
    Task ReceiveAuthorizationCode(string code, string? state, CancellationToken cancellationToken);
}
