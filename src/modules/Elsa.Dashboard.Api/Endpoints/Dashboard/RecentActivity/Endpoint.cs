using Elsa.Abstractions;
using Elsa.Dashboard.Api.Contracts;
using Elsa.Dashboard.Api.Models;
using Elsa.Dashboard.Api.Permissions;
using JetBrains.Annotations;

namespace Elsa.Dashboard.Api.Endpoints.Dashboard.RecentActivity;

[PublicAPI]
internal class Endpoint(IDashboardProvider dashboardProvider) : ElsaEndpointWithoutRequest<DashboardRecentActivityResponse>
{
    public override void Configure()
    {
        Get("/dashboard/recent-activity");
        ConfigurePermissions(DashboardPermissions.Read);
    }

    public override async Task<DashboardRecentActivityResponse> ExecuteAsync(CancellationToken cancellationToken)
    {
        var range = Query<string?>("range", false);
        var take = Query<int?>("take", false) ?? 20;
        var includeSystem = Query<bool>("includeSystem", false);
        return await dashboardProvider.GetRecentActivityAsync(new(range, includeSystem), take, cancellationToken);
    }
}
