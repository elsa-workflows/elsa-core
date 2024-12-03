using System.Net.Mime;
using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.Common.Serialization;
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
        var json = JsonSerializer.Serialize(response, SerializerOptions.ConfigurationJsonSerializerOptions);
        await SendStringAsync(json, contentType: MediaTypeNames.Application.Json, cancellation: ct);
    }
}