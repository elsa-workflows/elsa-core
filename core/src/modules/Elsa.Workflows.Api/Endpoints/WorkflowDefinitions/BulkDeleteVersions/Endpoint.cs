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

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkDeleteVersions;

[PublicAPI]
internal class BulkDeleteVersions(IWorkflowDefinitionManager workflowDefinitionManager, IAuthorizationService authorizationService)
    : ElsaEndpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/bulk-actions/delete/workflow-definitions/by-id");
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

        var store = HttpContext.RequestServices.GetRequiredService<IWorkflowDefinitionStore>();
        var definitions = (await store.FindManyAsync(new WorkflowDefinitionFilter { Ids = request.Ids }, cancellationToken)).ToList();
        var plan = await store.PlanDeletionAsync(definitions, cancellationToken);
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
