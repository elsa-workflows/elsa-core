using Elsa.Abstractions;
using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Abstractions.Models;
using Elsa.Dashboard.Api.Authorization;
using JetBrains.Annotations;

namespace Elsa.Dashboard.Api.Endpoints.Dashboard.RecentActivity;

[PublicAPI]
internal class Endpoint(IDashboardProvider dashboardProvider) : ElsaEndpointWithoutRequest<DashboardRecentActivityResponse>
{
    public override void Configure()
    {
        Get("/dashboard/recent-activity");
        RequireAuthenticatedOnly();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        if (!DashboardAccess.CanReadInstances(HttpContext))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        var range = Query<string?>("range", false);
        var take = Query<int?>("take", false) ?? 20;
        var includeSystem = Query<bool>("includeSystem", false);
        await Send.OkAsync(await dashboardProvider.GetRecentActivityAsync(new(range, includeSystem), take, cancellationToken), cancellationToken);
    }
}
