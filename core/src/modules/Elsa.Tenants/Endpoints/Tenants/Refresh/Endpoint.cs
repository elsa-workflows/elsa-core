using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Common.Multitenancy;

namespace Elsa.Tenants.Endpoints.Tenants.Refresh;

public class Endpoint(ITenantService tenantService) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/tenants/refresh");
        RequirePermission(Elsa.Tenants.Permissions.TenantPermissions.Tenants, "refresh");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await tenantService.RefreshAsync(ct);
        await Send.OkAsync(cancellation: ct);
    }
}