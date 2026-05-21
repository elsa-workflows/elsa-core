using Elsa.Abstractions;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Permissions;
using JetBrains.Annotations;

namespace Elsa.AI.Host.Endpoints.Ai.Tools;

[PublicAPI]
public class Endpoint(IAiToolRegistry toolRegistry) : ElsaEndpointWithoutRequest<IReadOnlyCollection<AiToolDefinition>>
{
    public override void Configure()
    {
        Get("/ai/tools");
        ConfigurePermissions(AiPermissions.ViewTools);
    }

    public override async Task<IReadOnlyCollection<AiToolDefinition>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return await toolRegistry.ListAsync(new AiToolQuery(), cancellationToken);
    }
}
