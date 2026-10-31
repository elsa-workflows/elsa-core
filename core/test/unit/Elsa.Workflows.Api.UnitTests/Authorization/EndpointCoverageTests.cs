using Elsa.Testing.Shared.Authorization;
using Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.List;

namespace Elsa.Workflows.Api.UnitTests.Authorization;

public class EndpointCoverageTests
{
    [Fact]
    public void EveryWorkflowsApiEndpointDeclaresItsAccess() =>
        EndpointCoverage.AssertEveryEndpointDeclaresAccess(typeof(List).Assembly);
}
