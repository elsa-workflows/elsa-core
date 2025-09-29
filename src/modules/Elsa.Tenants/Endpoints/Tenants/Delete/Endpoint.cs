using Elsa.Abstractions;
using Elsa.Common.Multitenancy;

namespace Elsa.Tenants.Endpoints.Tenants.Delete;

public class Endpoint(ITenantService tenantService, ITenantStore store) : ElsaEndpointWithoutRequest<Tenant>
{
    public override void Configure()
    {
        Delete("/tenants/{id}");
        ConfigurePermissions("delete:tenants");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<string>("id")!;
        var found = await store.DeleteAsync(id, ct);
        
        if (!found)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        
        await tenantService.RefreshAsync(ct);
        await Send.OkAsync(ct);
    }
}