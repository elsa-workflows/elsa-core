using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        await using var mutationLock = await WorkflowDefinitionMutationLock.AcquireAsync(HttpContext.RequestServices, cancellationToken);

        var authorizationResult = await authorizationService.AuthorizeAsync(User, new NotReadOnlyResource(), AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Succeeded)
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        var store = HttpContext.RequestServices.GetRequiredService<IWorkflowDefinitionStore>();
        var definition = (await store.FindAsync(new WorkflowDefinitionFilter { Id = request.Id }, cancellationToken))?.ShallowClone();
        if (definition is null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        var plan = await store.PlanDeletionAsync([definition], cancellationToken);
        if (!await authorizationService.CanDeleteAsync(User, plan))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        long deleted;
        try
        {
            deleted = await workflowDefinitionManager.DeleteAuthorizedAsync(plan, cancellationToken);
        }
        catch (WorkflowDefinitionEditConflictException)
        {
            AddError("The workflow definition changed or is protected.");
            await Send.ErrorsAsync(StatusCodes.Status409Conflict, cancellationToken);
            return;
        }

        if (deleted == 0)
            await Send.NotFoundAsync(cancellationToken);
        else
            await Send.NoContentAsync(cancellationToken);
    }
}
