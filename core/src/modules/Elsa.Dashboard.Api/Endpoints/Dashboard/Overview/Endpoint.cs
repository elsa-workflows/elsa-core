using Elsa.Abstractions;
using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Abstractions.Models;
using Elsa.Dashboard.Api.Authorization;
using JetBrains.Annotations;

namespace Elsa.Dashboard.Api.Endpoints.Dashboard.Overview;

[PublicAPI]
internal class Endpoint(IDashboardProvider dashboardProvider) : ElsaEndpointWithoutRequest<DashboardOverview>
{
    // Any signed-in caller may ask. The provider withholds each section the caller may not read, so the overview never
    // refuses a caller who can read part of it.
    public override void Configure()
    {
        Get("/dashboard/overview");
        RequireAuthenticatedOnly();
    }

    public override async Task<DashboardOverview> ExecuteAsync(CancellationToken cancellationToken)
    {
        var range = Query<string?>("range", false);
        var includeSystem = Query<bool>("includeSystem", false);
        return await dashboardProvider.GetOverviewAsync(new(range, includeSystem) { CanRead = DashboardAccess.CreateReadCheck(HttpContext) }, cancellationToken);
    }
}
