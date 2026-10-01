using Elsa.Testing.Shared.Authorization;
using DashboardApiFeature = Elsa.Dashboard.Api.Features.DashboardApiFeature;

namespace Elsa.Dashboard.Api.UnitTests;

public class EndpointCoverageTests
{
    [Fact]
    public void EveryDashboardEndpointDeclaresItsAccess() =>
        EndpointCoverage.AssertEveryEndpointDeclaresAccess(typeof(DashboardApiFeature).Assembly);
}
