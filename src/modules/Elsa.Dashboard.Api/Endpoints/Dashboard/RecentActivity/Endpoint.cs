using Elsa.Abstractions;
using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Abstractions.Models;
using Elsa.Dashboard.Api.Authorization;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Dashboard.Api.Endpoints.Dashboard.RecentActivity;

[PublicAPI]
internal class Endpoint(IDashboardProvider dashboardProvider) : ElsaEndpointWithoutRequest<DashboardRecentActivityResponse>
{
    public override void Configure()
    {
        Get("/dashboard/recent-activity");
        RequireAuthenticatedOnly();
        PreProcessor<RequireInstancesAccess<EmptyRequest>>();
    }

    public override async Task<DashboardRecentActivityResponse> ExecuteAsync(CancellationToken cancellationToken)
    {
        var range = Query<string?>("range", false);
        var take = Query<int?>("take", false) ?? 20;
        var includeSystem = Query<bool>("includeSystem", false);
        return await dashboardProvider.GetRecentActivityAsync(new(range, includeSystem) { CanRead = DashboardAccess.CreateReadCheck(HttpContext) }, take, cancellationToken);
    }
}
