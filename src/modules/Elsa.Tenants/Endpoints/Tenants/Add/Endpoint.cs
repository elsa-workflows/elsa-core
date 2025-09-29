using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.Common.Serialization;
using Elsa.Workflows;

namespace Elsa.Tenants.Endpoints.Tenants.Add;

public class Endpoint(ITenantService tenantService, IIdentityGenerator identityGenerator, ITenantStore tenantStore) : ElsaEndpoint<NewTenant, Tenant>
{
    public override void Configure()
    {
        Post("/tenants");
        ConfigurePermissions("write:tenants");
    }

    public override async Task HandleAsync(NewTenant req, CancellationToken ct)
    {
        var tenant = new Tenant
        {
            Id = req.IsDefault ? string.Empty : req.Id ?? identityGenerator.GenerateId(),
            TenantId = req.TenantId,
            Name = req.Name.Trim(),
            Configuration = Serializers.DeserializeConfiguration(req.Configuration)
        };
        await tenantStore.AddAsync(tenant, ct);
        await tenantService.RefreshAsync(ct);
        await Send.OkAsync(tenant, ct);
    }
}

public class NewTenant
{
    public bool IsDefault { get; set; }
    public string? Id { get; set; }
    public string? TenantId { get; set; }
    public string Name { get; set; } = default!;
    public JsonElement? Configuration { get; set; }
}