using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Agents.GenerateUniqueName;

/// Generates a unique name for an agent.
[UsedImplicitly]
public class Endpoint(IAgentManager agentManager) : ElsaEndpointWithoutRequest<GenerateUniqueNameResponse>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/ai/actions/agents/generate-unique-name");
        ConfigurePermissions("ai/agents:write");
    }

    /// <inheritdoc />
    public override async Task<GenerateUniqueNameResponse> ExecuteAsync(CancellationToken ct)
    {
        var newName = await agentManager.GenerateUniqueNameAsync(ct);
        return new(newName);
    }
}