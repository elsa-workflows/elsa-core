using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Filters;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Agents.IsUniqueName;

/// Checks if a name is unique.
[UsedImplicitly]
public class Endpoint(IAgentManager agentManager) : ElsaEndpoint<IsUniqueNameRequest, IsUniqueNameResponse>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/ai/queries/agents/is-unique-name");
        ConfigurePermissions("ai/agents:write");
    }

    /// <inheritdoc />
    public override async Task<IsUniqueNameResponse> ExecuteAsync(IsUniqueNameRequest req, CancellationToken ct)
    {
        var isUnique = await agentManager.IsNameUniqueAsync(req.Name, req.Id, ct);
        return new(isUnique);
    }
}