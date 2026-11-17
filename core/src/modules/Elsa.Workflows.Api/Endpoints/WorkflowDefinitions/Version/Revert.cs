using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Version;

[PublicAPI]
internal class RevertVersion(IWorkflowDefinitionManager workflowDefinitionManager, IAuthorizationService authorizationService, IWorkflowDefinitionStore store)
    : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Post("workflow-definitions/{definitionId}/revert/{version}");
        RequirePermission(Elsa.Workflows.Api.Permissions.WorkflowPermissions.DefinitionVersions, "revert");
    }

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

        var definition = await store.FindAsync(filter, cancellationToken);

        if (definition == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        var latest = await store.FindAsync(new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.Latest }, cancellationToken);
        var targets = latest is not null && latest.Id != definition.Id ? new[] { definition, latest } : [definition];
        if (!await authorizationService.CanMutateAsync(User, targets))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        var newDefinition = await workflowDefinitionManager.RevertVersionAsync(definitionId, version, cancellationToken);
        var newDefinitionSummary = WorkflowDefinitionSummary.FromDefinition(newDefinition);

        await Send.CreatedAtAsync("GetWorkflowDefinitionById", new
        {
            id = newDefinition.Id
        }, newDefinitionSummary, cancellation: cancellationToken);
    }
}