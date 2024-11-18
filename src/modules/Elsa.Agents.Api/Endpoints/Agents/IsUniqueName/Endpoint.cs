using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Agents.IsUniqueName;

/// <summary>
/// Checks if a name is unique.
/// </summary>
[UsedImplicitly]
public class Endpoint(IAgentManager agentManager) : ElsaEndpoint<IsUniqueNameRequest, IsUniqueNameResponse>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/ai/queries/agents/is-unique-name");
        ConfigurePermissions("ai/agents:read");
    }

    /// <inheritdoc />
    public override async Task<IsUniqueNameResponse> ExecuteAsync(IsUniqueNameRequest req, CancellationToken ct)
    {
        var isUnique = await agentManager.IsNameUniqueAsync(req.Name, req.Id, ct);
        return new(isUnique);
    }
}