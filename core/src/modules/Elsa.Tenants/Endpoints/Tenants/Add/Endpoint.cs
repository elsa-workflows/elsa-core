using Elsa.Authorization;
using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.Common.Serialization;
using Elsa.Workflows;
using Microsoft.AspNetCore.Http;

namespace Elsa.Tenants.Endpoints.Tenants.Add;

public class Endpoint(ITenantService tenantService, IIdentityGenerator identityGenerator, ITenantStore tenantStore) : ElsaEndpoint<NewTenant, Tenant>
{
    public override void Configure()
    {
        Post("/tenants");
        RequirePermission(Elsa.Tenants.Permissions.TenantPermissions.Tenants, CoreVerbs.Write);
    }

    public override async Task HandleAsync(NewTenant req, CancellationToken ct)
    {
        if (!TenantAdministration.HasHostAuthority(User))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        if (req.TenantId is { Length: > 0 } || req.Id is Tenant.AgnosticTenantId or "-" ||
            req.IsDefault && !string.IsNullOrEmpty(req.Id) || !req.IsDefault && req.Id is not null && string.IsNullOrWhiteSpace(req.Id))
        {
            AddError("Specify a tenant ID for a named tenant or select the default tenant. Registry entries belong to the host.");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
            return;
        }

        using var hostScope = TenantAdministration.BeginHostScope(HttpContext.RequestServices);
        var tenant = new Tenant
        {
            Id = req.IsDefault ? string.Empty : req.Id ?? identityGenerator.GenerateId(),
            Name = req.Name.Trim(),
            Configuration = Serializers.DeserializeConfiguration(req.Configuration)
        };
        await tenantStore.AddAsync(tenant, ct);
        await tenantService.RefreshAsync(ct);
        await Send.OkAsync(TenantAdministration.ToDirectoryEntry(tenant), ct);
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
