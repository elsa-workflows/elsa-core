using Elsa.Abstractions;
using Elsa.Dashboard.Api.Contracts;
using Elsa.Dashboard.Api.Models;
using Elsa.Dashboard.Api.Permissions;
using JetBrains.Annotations;

namespace Elsa.Dashboard.Api.Endpoints.Dashboard.Overview;

[PublicAPI]
internal class Endpoint(IDashboardProvider dashboardProvider) : ElsaEndpointWithoutRequest<DashboardOverview>
{
    public override void Configure()
    {
        Get("/dashboard/overview");
        ConfigurePermissions(DashboardPermissions.Read);
    }

    public override async Task<DashboardOverview> ExecuteAsync(CancellationToken cancellationToken)
    {
        var range = Query<string?>("range", false);
        var includeSystem = Query<bool>("includeSystem", false);
        return await dashboardProvider.GetOverviewAsync(new(range, includeSystem), cancellationToken);
    }
}
