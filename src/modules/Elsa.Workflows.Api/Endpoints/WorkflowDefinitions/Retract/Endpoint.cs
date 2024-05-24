using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Api.Services;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Retract;

[PublicAPI]
internal class Retract : ElsaEndpoint<Request, LinkedWorkflowDefinitionModel>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;
    private readonly IWorkflowDefinitionLinkService _linkService;
    private readonly IAuthorizationService _authorizationService;

    public Retract(IWorkflowDefinitionStore store, IWorkflowDefinitionPublisher workflowDefinitionPublisher, IWorkflowDefinitionLinkService linkService, IAuthorizationService authorizationService)
    {
        _store = store;
        _workflowDefinitionPublisher = workflowDefinitionPublisher;
        _linkService = linkService;
        _authorizationService = authorizationService;
    }

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

        var definition = await _store.FindAsync(filter, cancellationToken);

        if (definition == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var authorizationResult = _authorizationService.AuthorizeAsync(User, definition, AuthorizationPolicies.NotReadOnlyPolicy);

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

        await _workflowDefinitionPublisher.RetractAsync(definition, cancellationToken);
        var response = await _linkService.MapToLinkedWorkflowDefinitionModelAsync(definition, cancellationToken);
        await SendOkAsync(response, cancellationToken);
    }
}