using Elsa.Identity.Entities;
using Elsa.Identity.Implementations;

namespace Elsa.Identity.Services;

public interface IAccessTokenIssuer
{
    ValueTask<IssuedTokens> IssueTokensAsync(User user, CancellationToken cancellationToken = default);
}