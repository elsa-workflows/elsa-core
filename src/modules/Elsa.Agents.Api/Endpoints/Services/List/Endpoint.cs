using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Models;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Services.List;

/// Lists all registered API keys.
[UsedImplicitly]
public class Endpoint(IServiceStore store) : ElsaEndpointWithoutRequest<ListResponse<ServiceDefinition>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/ai/services");
        ConfigurePermissions("ai/services:read");
    }

    /// <inheritdoc />
    public override async Task<ListResponse<ServiceDefinition>> ExecuteAsync(CancellationToken ct)
    {
        var entities = await store.ListAsync(ct);
        return new ListResponse<ServiceDefinition>(entities.ToList());
    }
}