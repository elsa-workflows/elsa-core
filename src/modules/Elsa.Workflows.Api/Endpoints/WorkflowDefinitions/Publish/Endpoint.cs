using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Services;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Serialization.Converters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Publish;

[PublicAPI]
internal class Publish : ElsaEndpoint<Request, WorkflowDefinitionModel>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;
    private readonly IApiSerializer _serializer;
    private readonly WorkflowDefinitionMapper _workflowDefinitionMapper;
    private readonly IWorkflowDefinitionLinkService _linkService;

    public Publish(IWorkflowDefinitionStore store, IWorkflowDefinitionPublisher workflowDefinitionPublisher, IApiSerializer serializer, WorkflowDefinitionMapper workflowDefinitionMapper, IWorkflowDefinitionLinkService linkService)
    {
        _store = store;
        _workflowDefinitionPublisher = workflowDefinitionPublisher;
        _serializer = serializer;
        _workflowDefinitionMapper = workflowDefinitionMapper;
        _linkService = linkService;
    }

    public override void Configure()
    {
        Post("/workflow-definitions/{definitionId}/publish");
        ConfigurePermissions("publish:workflow-definitions");
        Policies(AuthorizationPolicies.NotReadOnlyPolicy);
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

        if (definition.IsPublished)
        {
            AddError($"Workflow with id {request.DefinitionId} is already published");
            await SendErrorsAsync(cancellation: cancellationToken);
            return;
        }

        await _workflowDefinitionPublisher.PublishAsync(definition, cancellationToken);

        var response = await _workflowDefinitionMapper.MapAsync(definition, cancellationToken);
        response = _linkService.GenerateLinksForSingleEntry(response);

        // We do not want to include composite root activities in the response.
        var serializerOptions = _serializer.GetOptions().Clone();
        serializerOptions.Converters.Add(new JsonIgnoreCompositeRootConverterFactory());

        await HttpContext.Response.WriteAsJsonAsync(response, serializerOptions, cancellationToken);
    }
}