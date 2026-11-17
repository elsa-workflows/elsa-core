using Elsa.Authorization;
using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.Common.Serialization;
using Microsoft.AspNetCore.Http;

namespace Elsa.Tenants.Endpoints.Tenants.Update;

public class Endpoint(ITenantService tenantService, ITenantStore tenantStore) : ElsaEndpoint<UpdatedTenant, Tenant>
{
    public override void Configure()
    {
        Post("/tenants/{id}");
        RequirePermission(Elsa.Tenants.Permissions.TenantPermissions.Tenants, CoreVerbs.Write);
    }

    public override async Task HandleAsync(UpdatedTenant req, CancellationToken ct)
    {
        if (!TenantAdministration.HasHostAuthority(User))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var id = TenantAdministration.NormalizeRouteId(Route<string>("id")!);
        using var hostScope = TenantAdministration.BeginHostScope(HttpContext.RequestServices);
        var existing = await tenantStore.FindAsync(id, ct);
        
        if (existing == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        
        if (req.TenantId is not null && !string.Equals(req.TenantId.NormalizeTenantId(), existing.TenantId.NormalizeTenantId(), StringComparison.Ordinal) ||
            !string.IsNullOrEmpty(req.Id) && !string.Equals(TenantAdministration.NormalizeRouteId(req.Id), id, StringComparison.Ordinal))
        {
            AddError("Tenant identifiers and ownership cannot be changed.");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
            return;
        }

        var tenant = new Tenant
        {
            Id = existing.Id,
            TenantId = existing.TenantId,
            Name = req.Name.Trim(),
            Configuration = req.Configuration is null ? existing.Configuration : Serializers.DeserializeConfiguration(req.Configuration)
        };
        await tenantStore.UpdateAsync(tenant, ct);
        await tenantService.RefreshAsync(ct);
        await Send.OkAsync(TenantAdministration.ToDirectoryEntry(tenant), ct);
    }
}

public class UpdatedTenant
{
    public string Id { get; set; } = default!;
    public string? TenantId { get; set; }
    public string Name { get; set; } = default!;
    public JsonElement? Configuration { get; set; }
}
