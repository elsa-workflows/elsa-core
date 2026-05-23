using Elsa.Abstractions;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Endpoints.AI;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Permissions;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.AI.Host.Endpoints.AI.Tools;

[PublicAPI]
public class Endpoint(IAIToolRegistry toolRegistry, IOptions<AIHostOptions> options) : ElsaEndpoint<Request, IReadOnlyCollection<AIToolDefinition>>
{
    public override void Configure()
    {
        Get("/ai/tools");
        ConfigurePermissions(AIPermissions.ViewTools);
    }

    public override async Task<IReadOnlyCollection<AIToolDefinition>> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var userPermissions = AIHttpContextIdentity.GetPermissions(HttpContext);
        return await toolRegistry.ListAsync(new AIToolQuery
        {
            Agent = AIHttpContextIdentity.GetAuthorizedAgent(request.Agent, options.Value, userPermissions),
            ActorId = AIHttpContextIdentity.GetActorId(HttpContext),
            TenantId = AIHttpContextIdentity.GetTenantId(HttpContext),
            UserPermissions = userPermissions
        }, cancellationToken);
    }
}

public class Request
{
    public string? Agent { get; set; }
}
