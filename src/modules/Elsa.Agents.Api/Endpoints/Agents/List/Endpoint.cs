using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Models;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Agents.List;

/// Lists all agents.
[UsedImplicitly]
public class Endpoint(IAgentStore store) : ElsaEndpointWithoutRequest<ListResponse<AgentDefinition>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/ai/agents");
        ConfigurePermissions("ai/agents:read");
    }

    /// <inheritdoc />
    public override async Task<ListResponse<AgentDefinition>> ExecuteAsync(CancellationToken ct)
    {
        var entities = await store.ListAsync(ct);
        return new ListResponse<AgentDefinition>(entities.ToList());
    }
}