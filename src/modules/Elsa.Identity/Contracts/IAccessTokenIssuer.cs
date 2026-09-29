using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Contracts;

public interface IAccessTokenIssuer
{
    ValueTask<IssuedTokens> IssueTokensAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues tokens whose refresh token continues the specified sign-in session, so that revoking the session also
    /// revokes it. A <c>null</c> <paramref name="sessionId"/> starts a new session.
    /// </summary>
    /// <remarks>
    /// The default implementation ignores <paramref name="sessionId"/> so that existing implementations keep
    /// compiling. Their refreshed tokens then start a new session each time, which leaves the refresh tokens a
    /// session held before its latest refresh valid when it is revoked.
    /// </remarks>
    ValueTask<IssuedTokens> IssueTokensAsync(User user, string? sessionId, CancellationToken cancellationToken = default) => IssueTokensAsync(user, cancellationToken);
}
