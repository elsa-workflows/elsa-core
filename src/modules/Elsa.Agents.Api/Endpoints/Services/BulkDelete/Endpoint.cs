using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Filters;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Services.BulkDelete;

/// <summary>
/// Deletes an API key.
/// </summary>
[UsedImplicitly]
public class Endpoint(IServiceStore store) : ElsaEndpoint<BulkDeleteRequest, BulkDeleteResponse>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/ai/bulk-actions/services/delete");
        ConfigurePermissions("ai/api-keys:delete");
    }

    /// <inheritdoc />
    public override async Task<BulkDeleteResponse> ExecuteAsync(BulkDeleteRequest req, CancellationToken ct)
    {
        var ids = req.Ids;
        var filter = new ServiceDefinitionFilter
        {
            Ids = ids
        };
        var count = await store.DeleteManyAsync(filter, ct);
        return new BulkDeleteResponse(count);
    }
}