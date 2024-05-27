using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Retract;

[PublicAPI]
internal class Retract(IWorkflowDefinitionStore store, IWorkflowDefinitionPublisher workflowDefinitionPublisher, IWorkflowDefinitionLinker linker, IAuthorizationService authorizationService)
    : ElsaEndpoint<Request, LinkedWorkflowDefinitionModel>
{
    public override void Configure()
    {
        Post("/workflow-definitions/{definitionId}/retract");
        ConfigurePermissions("retract:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = request.DefinitionId,
            VersionOptions = VersionOptions.LatestOrPublished
        };

        var definition = await store.FindAsync(filter, cancellationToken);

        if (definition == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var authorizationResult = authorizationService.AuthorizeAsync(User, new NotReadOnlyResource(definition), AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Result.Succeeded)
        {
            await SendForbiddenAsync(cancellationToken);
            return;
        }

        if (!definition.IsPublished)
        {
            AddError($"Workflow with id {request.DefinitionId} is not published");
            await SendErrorsAsync(cancellation: cancellationToken);
            return;
        }

        await workflowDefinitionPublisher.RetractAsync(definition, cancellationToken);
        var response = await linker.MapAsync(definition, cancellationToken);
        await SendOkAsync(response, cancellationToken);
    }
}