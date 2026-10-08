using Elsa.Identity.Models;

namespace Elsa.Identity.Contracts;

/// <summary>
/// Creates Elsa access and local refresh tokens from trusted issuance data.
/// </summary>
public interface IElsaTokenService
{
    /// <summary>
    /// Issues an Elsa access token.
    /// </summary>
    ValueTask<IssuedAccessToken> IssueAccessTokenAsync(TokenIssuanceContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a local Elsa refresh token.
    /// </summary>
    ValueTask<IssuedAccessToken> IssueRefreshTokenAsync(TokenIssuanceContext context, CancellationToken cancellationToken = default);
}
