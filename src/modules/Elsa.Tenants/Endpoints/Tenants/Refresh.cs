using Elsa.Abstractions;
using Elsa.Common.Multitenancy;

namespace Elsa.Tenants.Endpoints.Tenants;

public class Refresh(ITenantService tenantService) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/tenants/refresh");
        ConfigurePermissions("execute:refresh-tenants");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await tenantService.RefreshAsync(ct);
        await SendOkAsync(ct);
    }
}