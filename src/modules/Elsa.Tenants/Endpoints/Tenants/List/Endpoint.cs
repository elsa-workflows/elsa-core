using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.Models;

namespace Elsa.Tenants.Endpoints.Tenants.List;

public class Endpoint(ITenantService tenantService) : ElsaEndpointWithoutRequest<ListResponse<Tenant>>
{
    public override void Configure()
    {
        Get("/tenants");
        ConfigurePermissions("read:tenants");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var tenants = await tenantService.ListAsync(ct);
        var response = new ListResponse<Tenant>(tenants.ToList());
        await SendOkAsync(response, ct);
    }
}