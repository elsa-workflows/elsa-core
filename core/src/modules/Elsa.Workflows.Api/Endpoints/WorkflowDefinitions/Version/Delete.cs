using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

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
        var definitionId = Route<string>("definitionId")!;
        var version = Route<int>("version");

        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = definitionId,
            VersionOptions = VersionOptions.SpecificVersion(version)
        };

        var definition = await store.FindAsync(filter, cancellationToken);

        if (definition == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        if (!await authorizationService.CanDeleteVersionsAsync(User, store, [definition], cancellationToken))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        var result = await workflowDefinitionManager.DeleteVersionAsync(definition, cancellationToken);

        if (!result)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        await Send.OkAsync(cancellation: cancellationToken);
    }
}