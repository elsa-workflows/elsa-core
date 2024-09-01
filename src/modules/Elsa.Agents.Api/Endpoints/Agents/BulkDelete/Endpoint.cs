using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Filters;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Agents.BulkDelete;

/// Deletes an agent.
[UsedImplicitly]
public class Endpoint(IAgentManager agentManager) : ElsaEndpoint<BulkDeleteRequest, BulkDeleteResponse>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/ai/bulk-actions/agents/delete");
        ConfigurePermissions("ai/agents:delete");
    }

    /// <inheritdoc />
    public override async Task<BulkDeleteResponse> ExecuteAsync(BulkDeleteRequest req, CancellationToken ct)
    {
        var ids = req.Ids;
        var filter = new AgentDefinitionFilter
        {
            Ids = ids
        };
        var count = await agentManager.DeleteManyAsync(filter, ct);
        return new BulkDeleteResponse(count);
    }
}