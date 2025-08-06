using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.Common.Serialization;

namespace Elsa.Tenants.Endpoints.Tenants.Update;

public class Endpoint(ITenantService tenantService, ITenantStore tenantStore) : ElsaEndpoint<UpdatedTenant, Tenant>
{
    public override void Configure()
    {
        Post("/tenants/{id}");
        ConfigurePermissions("write:tenants");
    }

    public override async Task HandleAsync(UpdatedTenant req, CancellationToken ct)
    {
        var id = Route<string>("id")!;
        if (id == "-") id = string.Empty;
        var tenant = await tenantStore.FindAsync(id, ct);
        
        if (tenant == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        
        tenant.TenantId = req.TenantId;
        tenant.Name = req.Name.Trim();
        tenant.Configuration = Serializers.DeserializeConfiguration(req.Configuration);
        await tenantStore.UpdateAsync(tenant, ct);
        await tenantService.RefreshAsync(ct);
        await Send.OkAsync(tenant, ct);
    }
}

public class UpdatedTenant
{
    public string Id { get; set; } = default!;
    public string? TenantId { get; set; }
    public string Name { get; set; } = default!;
    public JsonElement? Configuration { get; set; }
}