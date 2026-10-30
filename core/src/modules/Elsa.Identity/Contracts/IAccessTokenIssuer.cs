using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Contracts;

public interface IAccessTokenIssuer
{
    ValueTask<IssuedTokens> IssueTokensAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues tokens whose refresh token continues the specified sign-in session, so that revoking the session also
    /// revokes it. A <c>null</c> <paramref name="session"/> starts a new session.
    /// </summary>
    /// <remarks>
    /// The refresh token must carry <paramref name="session"/>, for example by passing it to
    /// <see cref="IElsaTokenService.IssueRefreshTokenAsync"/> as <see cref="TokenIssuanceContext.Session"/>. One that
    /// does not starts a new session on every refresh, and revoking the session then leaves valid the refresh tokens
    /// it held before its latest refresh.
    /// </remarks>
    ValueTask<IssuedTokens> IssueTokensAsync(User user, SignInSession? session, CancellationToken cancellationToken = default);
}
