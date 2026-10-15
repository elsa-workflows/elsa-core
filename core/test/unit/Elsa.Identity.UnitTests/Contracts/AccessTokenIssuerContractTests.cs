using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.UnitTests.Contracts;

public class AccessTokenIssuerContractTests
{
    // A default implementation would let a custom issuer compile without carrying the session over, so that every
    // refresh silently started a new one and signing out left the earlier refresh tokens valid.
    [Fact]
    public void CustomIssuersMustImplementTheSessionAwareOverload()
    {
        var method = typeof(IAccessTokenIssuer).GetMethod(nameof(IAccessTokenIssuer.IssueTokensAsync), [typeof(User), typeof(SignInSession), typeof(CancellationToken)]);

        Assert.NotNull(method);
        Assert.True(method.IsAbstract);
    }
}
