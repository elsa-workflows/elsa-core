using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Filters;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Agents.GenerateUniqueName;

/// Lists all registered API keys.
[UsedImplicitly]
public class Endpoint(IAgentStore store) : ElsaEndpointWithoutRequest<GenerateUniqueNameResponse>
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
        var newName = await GenerateUniqueNameAsync(ct);
        return new(newName);
    }

    private async Task<string> GenerateUniqueNameAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 100;
        var attempt = 0;

        while (attempt < maxAttempts)
        {
            var name = $"Agent {++attempt}";
            var isUnique = await IsNameUniqueAsync(name, cancellationToken);

            if (isUnique)
                return name;
        }

        throw new Exception($"Failed to generate a unique workflow name after {maxAttempts} attempts.");
    }
    
    private async Task<bool> IsNameUniqueAsync(string name, CancellationToken ct)
    {
        var filter = new AgentDefinitionFilter
        {
            Name = name
        };
        return await store.FindAsync(filter, ct) == null;
    }
}