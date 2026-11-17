using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Version;

/// <summary>
/// Deletes a specific version of a workflow definition.
/// </summary>
[PublicAPI]
public class DeleteVersion(IWorkflowDefinitionManager workflowDefinitionManager, IWorkflowDefinitionStore store, IAuthorizationService authorizationService) : ElsaEndpointWithoutRequest
{
    /// <inheritdoc />
    public override void Configure()
    {
        Delete("workflow-definitions/{definitionId}/version/{version}");
        RequirePermission(Elsa.Workflows.Api.Permissions.WorkflowPermissions.DefinitionVersions, CoreVerbs.Delete);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        await using var mutationLock = await WorkflowDefinitionMutationLock.AcquireAsync(HttpContext.RequestServices, cancellationToken);

        var definitionId = Route<string>("definitionId")!;
        var version = Route<int>("version");

        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = definitionId,
            VersionOptions = VersionOptions.SpecificVersion(version)
        };

        var definition = (await store.FindAsync(filter, cancellationToken))?.ShallowClone();

        if (definition == null)
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
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        await Send.OkAsync(cancellation: cancellationToken);
    }
}
