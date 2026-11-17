using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Microsoft.AspNetCore.Http;

namespace Elsa.Tenants.Endpoints.Tenants.Delete;

public class Endpoint(ITenantService tenantService, ITenantStore store) : ElsaEndpointWithoutRequest<Tenant>
{
    public override void Configure()
    {
        Delete("/tenants/{id}");
        RequirePermission(Elsa.Tenants.Permissions.TenantPermissions.Tenants, CoreVerbs.Delete);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!TenantAdministration.HasHostAuthority(User))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var id = TenantAdministration.NormalizeRouteId(Route<string>("id")!);
        if (id == Tenant.DefaultTenantId)
        {
            AddError("The default tenant cannot be deleted.");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
            return;
        }

        using var hostScope = TenantAdministration.BeginHostScope(HttpContext.RequestServices);
        var found = await store.DeleteAsync(id, ct);
        
        if (!found)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        
        await tenantService.RefreshAsync(ct);
        await Send.OkAsync(cancellation: ct);
    }
}
