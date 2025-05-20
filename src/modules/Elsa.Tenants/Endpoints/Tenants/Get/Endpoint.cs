using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.Models;

namespace Elsa.Tenants.Endpoints.Tenants.Get;

public class Endpoint(ITenantService tenantService) : ElsaEndpointWithoutRequest<Tenant>
{
    public override void Configure()
    {
        Get("/tenants/{id}");
        ConfigurePermissions("read:tenants");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<string>("id")!;
        var tenant = await tenantService.FindAsync(id, ct);
        
        if (tenant == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }
        
        await SendOkAsync(tenant, ct);
    }
}