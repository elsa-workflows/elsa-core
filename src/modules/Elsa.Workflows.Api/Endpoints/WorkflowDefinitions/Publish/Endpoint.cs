using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Mappers;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Publish;

[PublicAPI]
internal class Publish : ElsaEndpoint<Request, WorkflowDefinitionResponse, WorkflowDefinitionMapper>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    public Publish(IWorkflowDefinitionStore store, IWorkflowDefinitionPublisher workflowDefinitionPublisher, SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _workflowDefinitionPublisher = workflowDefinitionPublisher;
        _serializerOptionsProvider = serializerOptionsProvider;
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

        if (definition.IsPublished)
        {
            AddError($"Workflow with id {request.DefinitionId} is already published");
            await SendErrorsAsync(cancellation: cancellationToken);
            return;
        }

        await _workflowDefinitionPublisher.PublishAsync(definition, cancellationToken);

        var response = await Map.FromEntityAsync(definition, cancellationToken);

        // We do not want to include composite root activities in the response.
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        serializerOptions.Converters.Add(new JsonIgnoreCompositeRootConverterFactory());

        await HttpContext.Response.WriteAsJsonAsync(response, serializerOptions, cancellationToken);
    }
}