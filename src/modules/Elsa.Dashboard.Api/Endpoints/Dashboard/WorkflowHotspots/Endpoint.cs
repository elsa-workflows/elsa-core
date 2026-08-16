using Elsa.Abstractions;
using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Abstractions.Models;
using Elsa.Dashboard.Api.Permissions;
using JetBrains.Annotations;

namespace Elsa.Dashboard.Api.Endpoints.Dashboard.WorkflowHotspots;

[PublicAPI]
internal class Endpoint(IDashboardProvider dashboardProvider) : ElsaEndpoint<DashboardWorkflowHotspotsRequest, DashboardWorkflowHotspotsResponse>
{
    public override void Configure()
    {
        Post("/dashboard/workflow-hotspots");
        ConfigurePermissions(DashboardPermissions.Read);
    }

    public override async Task<DashboardWorkflowHotspotsResponse> ExecuteAsync(DashboardWorkflowHotspotsRequest request, CancellationToken cancellationToken)
    {
        return await dashboardProvider.GetWorkflowHotspotsAsync(request, cancellationToken);
    }
}
