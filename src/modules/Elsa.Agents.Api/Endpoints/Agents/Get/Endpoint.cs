using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Extensions;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Agents.Get;

/// Gets an agent.
[UsedImplicitly]
public class Endpoint(IAgentManager agentManager) : ElsaEndpoint<Request, AgentModel>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/ai/agents/{id}");
        ConfigurePermissions("ai/agents:read");
    }

    /// <inheritdoc />
    public override async Task<AgentModel> ExecuteAsync(Request req, CancellationToken ct)
    {
        var entity = await agentManager.GetAsync(req.Id, ct);
        
        if(entity == null)
        {
            await SendNotFoundAsync(ct);
            return null!;
        }
        
        return entity.ToModel();
    }
}