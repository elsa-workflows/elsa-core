using Elsa.Abstractions;
using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Abstractions.Models;
using Elsa.Dashboard.Api.Authorization;
using JetBrains.Annotations;

namespace Elsa.Dashboard.Api.Endpoints.Dashboard.WorkflowTrends;

[PublicAPI]
internal class Endpoint(IDashboardProvider dashboardProvider) : ElsaEndpoint<DashboardTrendRequest, DashboardTrendResponse>
{
    public override void Configure()
    {
        Post("/dashboard/workflow-trends");
        RequireAuthenticatedOnly();
        PreProcessor<RequireInstancesAccess<DashboardTrendRequest>>();
    }

    public override async Task<DashboardTrendResponse> ExecuteAsync(DashboardTrendRequest request, CancellationToken cancellationToken)
    {
        return await dashboardProvider.GetWorkflowTrendsAsync(request with { CanRead = DashboardAccess.CreateReadCheck(HttpContext) }, cancellationToken);
    }
}
