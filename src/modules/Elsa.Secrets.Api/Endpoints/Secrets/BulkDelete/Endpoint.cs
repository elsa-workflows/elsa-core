using Elsa.Abstractions;
using Elsa.Secrets.BulkActions;
using Elsa.Secrets.Management;
using JetBrains.Annotations;

namespace Elsa.Secrets.Api.Endpoints.Secrets.BulkDelete;

/// Deletes an agent.
[UsedImplicitly]
public class Endpoint(ISecretManager manager) : ElsaEndpoint<BulkDeleteRequest, BulkDeleteResponse>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/bulk-actions/secrets/delete");
        ConfigurePermissions("secrets:delete");
    }

    /// <inheritdoc />
    public override async Task<BulkDeleteResponse> ExecuteAsync(BulkDeleteRequest req, CancellationToken ct)
    {
        var ids = req.Ids;
        var filter = new SecretFilter
        {
            Ids = ids
        };
        var count = await manager.DeleteManyAsync(filter, ct);
        return new BulkDeleteResponse(count);
    }
}