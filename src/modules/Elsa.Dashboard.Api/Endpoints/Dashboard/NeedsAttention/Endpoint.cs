using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Abstractions.Models;
using Elsa.Dashboard.Api.Permissions;
using JetBrains.Annotations;

namespace Elsa.Dashboard.Api.Endpoints.Dashboard.NeedsAttention;

[PublicAPI]
internal class Endpoint(IDashboardProvider dashboardProvider) : ElsaEndpointWithoutRequest<DashboardNeedsAttentionResponse>
{
    public override void Configure()
    {
        Get("/dashboard/needs-attention");
        RequirePermission(Elsa.Dashboard.Api.Permissions.DashboardResourcePermissions.Dashboard, CoreVerbs.View);
    }

    public override async Task<DashboardNeedsAttentionResponse> ExecuteAsync(CancellationToken cancellationToken)
    {
        var range = Query<string?>("range", false);
        var take = Query<int?>("take", false) ?? 8;
        var includeSystem = Query<bool>("includeSystem", false);
        return await dashboardProvider.GetNeedsAttentionAsync(new(range, includeSystem), take, cancellationToken);
    }
}
