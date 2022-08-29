using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Post;

public class Post : ElsaEndpoint<WorkflowDefinitionRequest, WorkflowDefinitionResponse>
{
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;
    private readonly VariableDefinitionMapper _variableDefinitionMapper;

    public Post(
        SerializerOptionsProvider serializerOptionsProvider,
        IWorkflowDefinitionPublisher workflowDefinitionPublisher,
        VariableDefinitionMapper variableDefinitionMapper)
    {
        _serializerOptionsProvider = serializerOptionsProvider;
        _workflowDefinitionPublisher = workflowDefinitionPublisher;
        _variableDefinitionMapper = variableDefinitionMapper;
    }

    public override void Configure()
    {
        Post("/workflow-definitions");
        ConfigurePermissions("write:workflow-definitions");
    }

    public override async Task HandleAsync(WorkflowDefinitionRequest request, CancellationToken cancellationToken)
    {
        var definitionId = request.DefinitionId;

        // Get a workflow draft version.
        var draft = !string.IsNullOrWhiteSpace(definitionId)
            ? await _workflowDefinitionPublisher.GetDraftAsync(definitionId, request.Version, cancellationToken)
            : default;

        var isNew = draft == null;

        // Create a new workflow in case no existing definition was found.
        if (isNew)
        {
            draft = _workflowDefinitionPublisher.New();

            if (!string.IsNullOrWhiteSpace(definitionId))
                draft.DefinitionId = definitionId;
        }

        // Update the draft with the received model.
        var root = request.Root ?? new Sequence();
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var stringData = JsonSerializer.Serialize(root, serializerOptions);
        var variables = _variableDefinitionMapper.Map(request.Variables).ToList();

        draft!.StringData = stringData;
        draft.MaterializerName = JsonWorkflowMaterializer.MaterializerName;
        draft.Name = request.Name?.Trim();
        draft.Description = request.Description?.Trim();
        draft.Metadata = request.Metadata ?? new Dictionary<string, object>();
        draft.Variables = variables;
        draft.ApplicationProperties = request.ApplicationProperties ?? new Dictionary<string, object>();
        draft = request.Publish ? await _workflowDefinitionPublisher.PublishAsync(draft, cancellationToken) : await _workflowDefinitionPublisher.SaveDraftAsync(draft, cancellationToken);

        var response = new WorkflowDefinitionResponse(
            draft.Id,
            draft.DefinitionId,
            draft.Name,
            draft.Description,
            draft.CreatedAt,
            draft.Version,
            request.Variables ?? new List<VariableDefinition>(),
            draft.Metadata,
            draft.ApplicationProperties,
            draft.IsLatest,
            draft.IsPublished,
            root);

        if (isNew)
            await SendCreatedAtAsync<Get.Get>(new { definitionId = definitionId }, response, cancellation: cancellationToken);
        else
            await SendOkAsync(response, cancellationToken);
    }
}