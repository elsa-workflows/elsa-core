using System.Text.Json;
using Elsa.Abstractions;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Agents.Invoke;

/// <summary>
/// Invokes an agent.
/// </summary>
[UsedImplicitly]
public class Execute(IAgentInvoker agentInvoker) : ElsaEndpoint<Request, JsonElement>
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
        var request = new InvokeAgentRequest
        {
            AgentName = req.Agent,
            Input = req.Inputs,
            CancellationToken = ct
        };
        var result = await agentInvoker.InvokeAsync(request).AsJsonElementAsync();
        return result;
    }
}