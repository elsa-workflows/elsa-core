using Elsa.Abstractions;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Endpoints.Ai;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Permissions;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.AI.Host.Endpoints.Ai.Tools;

[PublicAPI]
public class Endpoint(IAiToolRegistry toolRegistry, IOptions<AiHostOptions> options) : ElsaEndpoint<Request, IReadOnlyCollection<AiToolDefinition>>
{
    public override void Configure()
    {
        Get("/ai/tools");
        ConfigurePermissions(AiPermissions.ViewTools);
    }

    public override async Task<IReadOnlyCollection<AiToolDefinition>> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var userPermissions = AiHttpContextIdentity.GetPermissions(HttpContext);
        return await toolRegistry.ListAsync(new AiToolQuery
        {
            Agent = AiHttpContextIdentity.GetAuthorizedAgent(request.Agent, options.Value, userPermissions),
            ActorId = AiHttpContextIdentity.GetActorId(HttpContext),
            TenantId = AiHttpContextIdentity.GetTenantId(HttpContext),
            UserPermissions = userPermissions
        }, cancellationToken);
    }
}

public class Request
{
    public string? Agent { get; set; }
}
