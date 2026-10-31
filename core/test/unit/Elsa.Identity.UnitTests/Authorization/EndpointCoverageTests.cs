using Elsa.Identity.Services;
using Elsa.Testing.Shared.Authorization;

namespace Elsa.Identity.UnitTests.Authorization;

public class EndpointCoverageTests
{
    [Fact]
    public void EveryIdentityEndpointDeclaresItsAccess() =>
        EndpointCoverage.AssertEveryEndpointDeclaresAccess(typeof(RoleAuthorizationService).Assembly);
}
