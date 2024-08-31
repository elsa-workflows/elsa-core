using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Extensions;
using Elsa.Models;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Agents.List;

/// Lists all agents.
[UsedImplicitly]
public class Endpoint(IAgentManager agentManager) : ElsaEndpointWithoutRequest<ListResponse<AgentModel>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/ai/agents");
        ConfigurePermissions("ai/agents:read");
    }

    /// <inheritdoc />
    public override async Task<ListResponse<AgentModel>> ExecuteAsync(CancellationToken ct)
    {
        var entities = await agentManager.ListAsync(ct);
        var models = entities.Select(x => x.ToModel()).ToList();
        return new ListResponse<AgentModel>(models);
    }
}