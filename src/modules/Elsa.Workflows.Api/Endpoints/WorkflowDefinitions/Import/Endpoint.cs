using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Import;

public class Import : ElsaEndpoint<WorkflowDefinitionRequest, WorkflowDefinitionResponse>
{
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly VariableDefinitionMapper _variableDefinitionMapper;

    public Import(
        SerializerOptionsProvider serializerOptionsProvider,
        IWorkflowDefinitionPublisher workflowDefinitionPublisher,
        IWorkflowDefinitionService workflowDefinitionService,
        VariableDefinitionMapper variableDefinitionMapper)
    {
        _serializerOptionsProvider = serializerOptionsProvider;
        _workflowDefinitionPublisher = workflowDefinitionPublisher;
        _workflowDefinitionService = workflowDefinitionService;
        _variableDefinitionMapper = variableDefinitionMapper;
    }

    public override void Configure()
    {
        Post("workflow-definitions/import", "workflow-definitions/{definitionId}/import");
        ConfigurePermissions("write:workflow-definitions");
    }

    public override async Task HandleAsync(WorkflowDefinitionRequest request, CancellationToken cancellationToken)
    {
        var definitionId = request.DefinitionId;

        // Get a workflow draft version.
        var draft = !string.IsNullOrWhiteSpace(definitionId)
            ? await _workflowDefinitionPublisher.GetDraftAsync(definitionId, cancellationToken:cancellationToken)
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
        var root = request.Root;
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var stringData = JsonSerializer.Serialize(root, serializerOptions);
        var variables = _variableDefinitionMapper.Map(request.Variables).ToList();

        draft!.StringData = stringData;
        draft.MaterializerName = JsonWorkflowMaterializer.MaterializerName;
        draft.Name = request.Name?.Trim();
        draft.Description = request.Description?.Trim();
        draft.Metadata = request.Metadata;
        draft.Variables = variables;
        draft.ApplicationProperties = request.ApplicationProperties;
        draft = request.Publish ? await _workflowDefinitionPublisher.PublishAsync(draft, cancellationToken) : await _workflowDefinitionPublisher.SaveDraftAsync(draft, cancellationToken);

        // Materialize the workflow definition for serialization.
        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(draft, cancellationToken);

        var response = new WorkflowDefinitionResponse(
            draft.Id,
            draft.DefinitionId,
            draft.Name,
            draft.Description,
            draft.CreatedAt,
            draft.Version,
            request.Variables,
            draft.Metadata,
            draft.ApplicationProperties,
            draft.IsLatest,
            draft.IsPublished,
            workflow.Root);

        if (isNew)
            await SendCreatedAtAsync<Get.Get>(new { DefinitionId = definitionId }, response, cancellation: cancellationToken);
        else
            await SendOkAsync(response, cancellationToken);
    }
}