using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.DeleteVersion;

[PublicAPI]
internal class DeleteVersion(IWorkflowDefinitionManager workflowDefinitionManager, IAuthorizationService authorizationService)
    : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Delete("/workflow-definition-versions/{id}");
        RequirePermission(Elsa.Workflows.Api.Permissions.WorkflowPermissions.Definitions, CoreVerbs.Delete);
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(User, new NotReadOnlyResource(), AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Succeeded)
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        var store = HttpContext.RequestServices.GetRequiredService<IWorkflowDefinitionStore>();
        var definition = await store.FindAsync(new WorkflowDefinitionFilter { Id = request.Id }, cancellationToken);
        if (definition is null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        if (!await authorizationService.CanDeleteVersionsAsync(User, store, [definition], cancellationToken))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        var deleted = await workflowDefinitionManager.DeleteVersionAsync(definition, cancellationToken);

        if (!deleted)
            await Send.NotFoundAsync(cancellationToken);
        else
            await Send.NoContentAsync(cancellationToken);
    }
}