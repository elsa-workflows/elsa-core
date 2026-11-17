using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Workflows.Api.Constants;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkDelete;

[UsedImplicitly]
internal class BulkDelete(IWorkflowDefinitionManager workflowDefinitionManager, IAuthorizationService authorizationService, IWorkflowDefinitionStore store)
    : ElsaEndpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/bulk-actions/delete/workflow-definitions/by-definition-id");
        RequirePermission(Elsa.Workflows.Api.Permissions.WorkflowPermissions.Definitions, CoreVerbs.Delete);
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        await using var mutationLock = await WorkflowDefinitionMutationLock.AcquireAsync(HttpContext.RequestServices, cancellationToken);

        var authorizationResult = await authorizationService.AuthorizeAsync(User, new NotReadOnlyResource(), AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Succeeded)
        {
            await Send.ForbiddenAsync(cancellationToken);
            return null!;
        }

        var definitions = (await store.FindManyAsync(new WorkflowDefinitionFilter { DefinitionIds = request.DefinitionIds }, cancellationToken)).ToList();
        var plan = new WorkflowDefinitionDeletionPlan(definitions, definitions);
        if (!await authorizationService.CanDeleteAsync(User, plan))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return null!;
        }

        try
        {
            return new Response(await workflowDefinitionManager.DeleteAuthorizedAsync(plan, cancellationToken));
        }
        catch (WorkflowDefinitionEditConflictException)
        {
            AddError("The workflow definitions changed or are protected.");
            await Send.ErrorsAsync(StatusCodes.Status409Conflict, cancellationToken);
            return null!;
        }
    }
}
