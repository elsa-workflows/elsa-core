using Elsa.Abstractions;
using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Abstractions.Models;
using Elsa.Dashboard.Api.Authorization;
using JetBrains.Annotations;

namespace Elsa.Dashboard.Api.Endpoints.Dashboard.WorkflowHotspots;

[PublicAPI]
internal class Endpoint(IDashboardProvider dashboardProvider) : ElsaEndpoint<DashboardWorkflowHotspotsRequest, DashboardWorkflowHotspotsResponse>
{
    public override void Configure()
    {
        Post("/dashboard/workflow-hotspots");
        RequireAuthenticatedOnly();
    }

    public override async Task HandleAsync(DashboardWorkflowHotspotsRequest request, CancellationToken cancellationToken)
    {
        if (!DashboardAccess.CanReadInstances(HttpContext))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        await Send.OkAsync(await dashboardProvider.GetWorkflowHotspotsAsync(request, cancellationToken), cancellationToken);
    }
}
