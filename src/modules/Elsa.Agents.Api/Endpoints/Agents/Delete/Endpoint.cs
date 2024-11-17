using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Agents.Delete;

/// <summary>
/// Deletes an Agent.
/// </summary>
[UsedImplicitly]
public class Endpoint(IAgentManager agentManager) : ElsaEndpoint<Request>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/ai/agents/{id}");
        ConfigurePermissions("ai/agents:delete");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var entity = await agentManager.GetAsync(req.Id, ct);
        
        if(entity == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }
        
        await agentManager.DeleteAsync(entity, ct);
    }
}