using Elsa.Api.Client.Resources.Identity.Responses;

namespace Elsa.Studio.Login.Contracts;

/// <summary>
/// Provides a service to refresh an access_token from an authorization server
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Refreshes the access_token
    /// </summary>
    Task<LoginResponse> RefreshTokenAsync(CancellationToken cancellationToken);
}
