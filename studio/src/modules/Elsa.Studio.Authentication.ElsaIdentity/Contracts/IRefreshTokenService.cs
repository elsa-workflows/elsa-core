using Elsa.Api.Client.Resources.Identity.Responses;

namespace Elsa.Studio.Authentication.ElsaIdentity.Contracts;

/// <summary>
/// Refreshes access tokens when the backend issues refresh tokens.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Refreshes the current token.
    /// </summary>
    Task<LoginResponse> RefreshTokenAsync(CancellationToken cancellationToken);
}

