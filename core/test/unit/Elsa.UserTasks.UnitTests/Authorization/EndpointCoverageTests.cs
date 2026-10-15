using Elsa.Testing.Shared.Authorization;
using Elsa.UserTasks.Services;

namespace Elsa.UserTasks.UnitTests.Authorization;

public class EndpointCoverageTests
{
    [Fact]
    public void EveryUserTasksEndpointDeclaresItsAccess() =>
        EndpointCoverage.AssertEveryEndpointDeclaresAccess(typeof(DefaultUserTaskAccessPolicy).Assembly);
}
