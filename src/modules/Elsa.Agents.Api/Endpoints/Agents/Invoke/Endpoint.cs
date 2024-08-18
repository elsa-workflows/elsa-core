using System.Text.Json;
using Elsa.Abstractions;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Agents.Invoke;

/// Invokes an agent.
[UsedImplicitly]
public class Execute(AgentInvoker agentInvoker) : ElsaEndpoint<Request, JsonElement>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/ai/agents/{agent}/invoke");
        ConfigurePermissions("ai/agents:invoke");
    }

    /// <inheritdoc />
    public override async Task<JsonElement> ExecuteAsync(Request req, CancellationToken ct)
    {
        var result = await agentInvoker.InvokeAgentAsync(req.Agent, req.Inputs, ct).AsJsonElementAsync();
        return result;
    }
}