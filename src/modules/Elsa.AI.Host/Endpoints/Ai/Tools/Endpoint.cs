using Elsa.Abstractions;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Endpoints.Ai;
using Elsa.AI.Host.Permissions;
using JetBrains.Annotations;

namespace Elsa.AI.Host.Endpoints.Ai.Tools;

[PublicAPI]
public class Endpoint(IAiToolRegistry toolRegistry) : ElsaEndpoint<Request, IReadOnlyCollection<AiToolDefinition>>
{
    public override void Configure()
    {
        Get("/ai/tools");
        ConfigurePermissions(AiPermissions.ViewTools);
    }

    public override async Task<IReadOnlyCollection<AiToolDefinition>> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        return await toolRegistry.ListAsync(new AiToolQuery
        {
            Agent = request.Agent,
            ActorId = AiHttpContextIdentity.GetActorId(HttpContext),
            TenantId = AiHttpContextIdentity.GetTenantId(HttpContext),
            UserPermissions = AiHttpContextIdentity.GetPermissions(HttpContext)
        }, cancellationToken);
    }
}

public class Request
{
    public string? Agent { get; set; }
}
