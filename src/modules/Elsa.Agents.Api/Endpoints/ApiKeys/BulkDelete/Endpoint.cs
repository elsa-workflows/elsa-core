using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Filters;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.ApiKeys.BulkDelete;

/// <summary>
/// Deletes an API key.
/// </summary>
[UsedImplicitly]
public class Endpoint(IApiKeyStore store) : ElsaEndpoint<BulkDeleteRequest, BulkDeleteResponse>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/ai/bulk-actions/api-keys/delete");
        ConfigurePermissions("ai/api-keys:delete");
    }

    /// <inheritdoc />
    public override async Task<BulkDeleteResponse> ExecuteAsync(BulkDeleteRequest req, CancellationToken ct)
    {
        var ids = req.Ids;
        var filter = new ApiKeyDefinitionFilter
        {
            Ids = ids
        };
        var count = await store.DeleteManyAsync(filter, ct);
        return new BulkDeleteResponse(count);
    }
}