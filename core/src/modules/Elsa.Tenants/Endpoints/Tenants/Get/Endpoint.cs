using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.Models;

namespace Elsa.Tenants.Endpoints.Tenants.Get;

public class Endpoint(ITenantService tenantService) : ElsaEndpointWithoutRequest<Tenant>
{
    public override void Configure()
    {
        Get("/tenants/{id}");
        RequirePermission(Elsa.Tenants.Permissions.TenantPermissions.Tenants, CoreVerbs.View);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!TenantAdministration.HasHostAuthority(User))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var id = TenantAdministration.NormalizeRouteId(Route<string>("id")!);
        var tenant = await tenantService.FindAsync(id, ct);
        
        if (tenant == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        
        await Send.OkAsync(TenantAdministration.ToDirectoryEntry(tenant), ct);
    }
}
