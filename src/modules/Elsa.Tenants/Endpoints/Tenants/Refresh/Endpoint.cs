using Elsa.Abstractions;
using Elsa.Common.Multitenancy;

namespace Elsa.Tenants.Endpoints.Tenants.Refresh;

public class Endpoint(ITenantService tenantService) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/tenants/refresh");
        ConfigurePermissions("execute:tenants:refresh");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await tenantService.RefreshAsync(ct);
        await SendOkAsync(ct);
    }
}