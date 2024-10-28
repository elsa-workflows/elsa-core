using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Models;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.ApiKeys.List;

/// Lists all registered API keys.
[UsedImplicitly]
public class Endpoint(IApiKeyStore store) : ElsaEndpointWithoutRequest<ListResponse<ApiKeyModel>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/ai/api-keys");
        ConfigurePermissions("ai/api-keys:read");
    }

    /// <inheritdoc />
    public override async Task<ListResponse<ApiKeyModel>> ExecuteAsync(CancellationToken ct)
    {
        var entities = await store.ListAsync(ct);
        var models = entities.Select(x => x.ToModel()).ToList();
        return new ListResponse<ApiKeyModel>(models);
    }
}