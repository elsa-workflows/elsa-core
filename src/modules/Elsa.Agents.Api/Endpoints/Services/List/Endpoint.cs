using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Models;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Services.List;

/// Lists all registered API keys.
[UsedImplicitly]
public class Endpoint(IServiceStore store) : ElsaEndpointWithoutRequest<ListResponse<ServiceModel>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/ai/services");
        ConfigurePermissions("ai/services:read");
    }

    /// <inheritdoc />
    public override async Task<ListResponse<ServiceModel>> ExecuteAsync(CancellationToken ct)
    {
        var entities = await store.ListAsync(ct);
        var models = entities.Select(x => x.ToModel()).ToList();
        return new ListResponse<ServiceModel>(models);
    }
}