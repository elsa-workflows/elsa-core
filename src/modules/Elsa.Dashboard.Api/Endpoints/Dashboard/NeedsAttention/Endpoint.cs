using Elsa.Abstractions;
using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Abstractions.Models;
using Elsa.Dashboard.Api.Authorization;
using JetBrains.Annotations;

namespace Elsa.Dashboard.Api.Endpoints.Dashboard.NeedsAttention;

[PublicAPI]
internal class Endpoint(IDashboardProvider dashboardProvider) : ElsaEndpointWithoutRequest<DashboardNeedsAttentionResponse>
{
    public override void Configure()
    {
        Get("/dashboard/needs-attention");
        RequireAuthenticatedOnly();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var canRead = DashboardAccess.CreateReadCheck(HttpContext);

        if (!canRead(DashboardAccess.WorkflowInstances))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        var range = Query<string?>("range", false);
        var take = Query<int?>("take", false) ?? 8;
        var includeSystem = Query<bool>("includeSystem", false);
        await Send.OkAsync(await dashboardProvider.GetNeedsAttentionAsync(new(range, includeSystem) { CanRead = canRead }, take, cancellationToken), cancellationToken);
    }
}
