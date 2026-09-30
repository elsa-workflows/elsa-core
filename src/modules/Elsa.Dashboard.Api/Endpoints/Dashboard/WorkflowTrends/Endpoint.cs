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
    }

    public override async Task HandleAsync(DashboardTrendRequest request, CancellationToken cancellationToken)
    {
        if (!DashboardAccess.CanReadInstances(HttpContext))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        await Send.OkAsync(await dashboardProvider.GetWorkflowTrendsAsync(request, cancellationToken), cancellationToken);
    }
}
