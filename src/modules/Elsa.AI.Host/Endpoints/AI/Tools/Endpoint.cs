using Elsa.Authorization;
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
        RequirePermission(Elsa.AI.Host.Permissions.AIResourcePermissions.Tools, CoreVerbs.View);
    }

    public override async Task<IReadOnlyCollection<AIToolDefinition>> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var userPermissions = AIHttpContextIdentity.GetPermissions(HttpContext);
        return await toolRegistry.ListAsync(new AIToolQuery
        {
            // HttpContext?.User, not HttpContext.User: unlike the chat endpoint, this method never dereferences
            // HttpContext unconditionally, and the sibling calls below all accept a null context — so it really
            // can be null here. GetAuthorizedAgent treats a null principal as holding nothing, while an agent
            // that declares no required permissions stays authorized either way.
            Agent = AIHttpContextIdentity.GetAuthorizedAgent(request.Agent, options.Value, HttpContext?.User),
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
