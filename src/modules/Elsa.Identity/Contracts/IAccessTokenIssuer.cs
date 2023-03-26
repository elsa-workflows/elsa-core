using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Contracts;

public interface IAccessTokenIssuer
{
    ValueTask<IssuedTokens> IssueTokensAsync(User user, CancellationToken cancellationToken = default);
}