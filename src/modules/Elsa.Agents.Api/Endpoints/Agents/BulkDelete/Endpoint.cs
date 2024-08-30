using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Filters;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Agents.BulkDelete;

/// Deletes an Agent.
[UsedImplicitly]
public class Endpoint(IAgentStore store) : ElsaEndpoint<Request, Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/ai/bulk-actions/agents/delete");
        ConfigurePermissions("ai/agents:delete");
    }

    /// <inheritdoc />
    public override async Task<Response> ExecuteAsync(Request req, CancellationToken ct)
    {
        var ids = req.Ids;
        var filter = new AgentDefinitionFilter
        {
            Ids = ids
        };
        var count = await store.DeleteManyAsync(filter, ct);
        return new Response(count);
    }
}