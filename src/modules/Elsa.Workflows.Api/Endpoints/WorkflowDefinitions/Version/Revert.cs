using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
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
        ConfigurePermissions("publish:workflow-definitions");
    }

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
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var authorizationResult = await authorizationService.AuthorizeAsync(User, new NotReadOnlyResource(definition), AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Succeeded)
        {
            await SendForbiddenAsync(cancellationToken);
            return;
        }

        var newDefinition = await workflowDefinitionManager.RevertVersionAsync(definitionId, version, cancellationToken);
        var newDefinitionSummary = WorkflowDefinitionSummary.FromDefinition(newDefinition);

        await SendCreatedAtAsync("GetWorkflowDefinitionById", new
        {
            id = newDefinition.Id
        }, newDefinitionSummary, cancellation: cancellationToken);
    }
}