using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Api.Services;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Serialization.Converters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Publish;

[PublicAPI]
internal class Publish : ElsaEndpoint<Request, LinkedWorkflowDefinitionModel>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;
    private readonly IApiSerializer _serializer;
    private readonly IWorkflowDefinitionLinkService _linkService;
    private readonly IAuthorizationService _authorizationService;

    public Publish(IWorkflowDefinitionStore store, IWorkflowDefinitionPublisher workflowDefinitionPublisher, IApiSerializer serializer, IWorkflowDefinitionLinkService linkService, IAuthorizationService authorizationService)
    {
        _store = store;
        _workflowDefinitionPublisher = workflowDefinitionPublisher;
        _serializer = serializer;
        _linkService = linkService;
        _authorizationService = authorizationService;
    }

    public override void Configure()
    {
        Post("/workflow-definitions/{definitionId}/publish");
        ConfigurePermissions("publish:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = request.DefinitionId,
            VersionOptions = VersionOptions.Latest
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

        if (definition.IsPublished)
        {
            AddError($"Workflow with id {request.DefinitionId} is already published");
            await SendErrorsAsync(cancellation: cancellationToken);
            return;
        }

        await _workflowDefinitionPublisher.PublishAsync(definition, cancellationToken);

        var response = await _linkService.MapToLinkedWorkflowDefinitionModelAsync(definition, cancellationToken);

        // We do not want to include composite root activities in the response.
        var serializerOptions = _serializer.GetOptions().Clone();
        serializerOptions.Converters.Add(new JsonIgnoreCompositeRootConverterFactory());

        await HttpContext.Response.WriteAsJsonAsync(response, serializerOptions, cancellationToken);
    }
}