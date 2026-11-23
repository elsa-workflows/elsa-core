using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Delete;

internal class Delete(IWorkflowDefinitionManager workflowDefinitionManager, IAuthorizationService authorizationService, IWorkflowDefinitionStore store)
    : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Delete("/workflow-definitions/{definitionId}");
        RequirePermission(Elsa.Workflows.Api.Permissions.WorkflowPermissions.Definitions, CoreVerbs.Delete);
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        await using var mutationLock = await WorkflowDefinitionMutationLock.AcquireAsync(HttpContext.RequestServices, cancellationToken);

        var filter = new WorkflowDefinitionFilter { DefinitionId = request.DefinitionId };
        var versions = (await store.FindManyAsync(filter, cancellationToken)).ToList();

        if (!versions.Any(definition => definition.IsLatest))
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        var plan = new WorkflowDefinitionDeletionPlan(versions, versions);
        if (!await authorizationService.CanDeleteAsync(User, plan))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        long result;
        try
        {
            result = await workflowDefinitionManager.DeleteAuthorizedAsync(plan, cancellationToken);
        }
        catch (WorkflowDefinitionEditConflictException)
        {
            AddError("The workflow definition changed or is protected.");
            await Send.ErrorsAsync(StatusCodes.Status409Conflict, cancellationToken);
            return;
        }

        if (result == 0)
            await Send.NotFoundAsync(cancellationToken);
        else
            await Send.NoContentAsync(cancellationToken);
    }
}
