using Elsa.Identity.Services;
using Elsa.Testing.Shared.Authorization;

namespace Elsa.Identity.UnitTests.Authorization;

public class EndpointCoverageTests
{
    [Test]
    public void EveryIdentityEndpointDeclaresItsAccess() =>
        EndpointCoverage.AssertEveryEndpointDeclaresAccess(typeof(RoleAuthorizationService).Assembly);
}