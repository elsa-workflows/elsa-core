using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore.Attributes;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "Post")]
[ProducesResponseType(typeof(WorkflowDefinition), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(WorkflowDefinition), StatusCodes.Status201Created)]
public class Post : Controller
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

    public record SaveWorkflowDefinitionRequest(
        string? DefinitionId,
        string? Name,
        string? Description,
        IActivity? Root,
        ICollection<VariableDefinition>? Variables,
        IDictionary<string, object>? Metadata,
        IDictionary<string, object>? ApplicationProperties,
        bool Publish);

    [HttpPost]
    public async Task<IActionResult> HandleAsync(CancellationToken cancellationToken)
    {
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var model = (await Request.ReadFromJsonAsync<SaveWorkflowDefinitionRequest>(serializerOptions, cancellationToken))!;
        var definitionId = model.DefinitionId;

        // Get a workflow draft version.
        var draft = !string.IsNullOrWhiteSpace(definitionId)
            ? await _workflowDefinitionPublisher.GetDraftAsync(definitionId, cancellationToken)
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
        var root = model.Root ?? new Sequence();
        var stringData = JsonSerializer.Serialize(root, serializerOptions);
        var variables = _variableDefinitionMapper.Map(model.Variables).ToList();

        draft!.StringData = stringData;
        draft.MaterializerName = JsonWorkflowMaterializer.MaterializerName;
        draft.Name = model.Name?.Trim();
        draft.Description = model.Description?.Trim();
        draft.Metadata = model.Metadata ?? new Dictionary<string, object>();
        draft.Variables = variables;
        draft.ApplicationProperties = model.ApplicationProperties ?? new Dictionary<string, object>();
        draft = model.Publish ? await _workflowDefinitionPublisher.PublishAsync(draft, cancellationToken) : await _workflowDefinitionPublisher.SaveDraftAsync(draft, cancellationToken);

        var draftModel = new WorkflowDefinitionModel(
            draft.Id,
            draft.DefinitionId,
            draft.Name,
            draft.Description,
            draft.CreatedAt,
            draft.Version,
            model.Variables ?? new List<VariableDefinition>(),
            draft.Metadata,
            draft.ApplicationProperties,
            draft.IsLatest,
            draft.IsPublished,
            root);

        var result = Json(draftModel, serializerOptions);
        result.StatusCode = isNew ? StatusCodes.Status201Created : StatusCodes.Status200OK;

        return result;
    }
}