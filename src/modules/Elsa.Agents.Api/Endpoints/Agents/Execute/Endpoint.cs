using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Agents.Activities;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Agents.Execute;

/// <summary>
/// Executes a function of a skilled agent.
/// </summary>
[UsedImplicitly]
public class Execute(AgentInvoker agentInvoker) : ElsaEndpoint<Request, JsonElement>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/agents/{agent}/execute/{skill}/{function}");
    }

    /// <inheritdoc />
    public override async Task<JsonElement> ExecuteAsync(Request req, CancellationToken ct)
    {
        var result = await agentInvoker.InvokeAgentAsync(req.Agent, req.Inputs, ct).AsJsonElementAsync();
        return result;
    }
}