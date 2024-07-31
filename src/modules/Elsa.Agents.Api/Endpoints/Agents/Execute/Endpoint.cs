using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Agents;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Agents.Execute;

/// <summary>
/// Executes a function of a skilled agent.
/// </summary>
[UsedImplicitly]
public class Execute(AgentManager agentManager) : ElsaEndpoint<Request, JsonElement>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/agents/{agent}/execute/{skill}/{function}");
    }

    /// <inheritdoc />
    public override async Task<JsonElement> ExecuteAsync(Request req, CancellationToken ct)
    {
        var agent = agentManager.GetAgent(req.Agent);
        var result = await agent.ExecuteAsync(req.Skill, req.Function, req.Inputs, ct).AsJsonElementAsync();
        return result;
    }
}