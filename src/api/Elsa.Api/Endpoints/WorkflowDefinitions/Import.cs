using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Api.Models;
using Elsa.AspNetCore;
using Elsa.Management.Materializers;
using Elsa.Management.Services;
using Elsa.Persistence.Entities;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "Import")]
[ProducesResponseType(typeof(WorkflowDefinition), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(WorkflowDefinition), StatusCodes.Status201Created)]
public class Import : Controller
{
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;
    private readonly IWorkflowPublisher _workflowPublisher;

    public Import(WorkflowSerializerOptionsProvider serializerOptionsProvider, IWorkflowPublisher workflowPublisher)
    {
        _serializerOptionsProvider = serializerOptionsProvider;
        _workflowPublisher = workflowPublisher;
    }
    
    [HttpPost]
    public async Task<IActionResult> HandleAsync(string? definitionId, bool publish, CancellationToken cancellationToken)
    {
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var model = (await Request.ReadFromJsonAsync<WorkflowDefinitionModel>(serializerOptions, cancellationToken))!;

        // Get a workflow draft version.
        var draft = !string.IsNullOrWhiteSpace(definitionId)
            ? await _workflowPublisher.GetDraftAsync(definitionId, cancellationToken)
            : default;

        var isNew = draft == null;

        // Create a new workflow in case no existing definition was found.
        if (isNew)
        {
            draft = _workflowPublisher.New();

            if (!string.IsNullOrWhiteSpace(definitionId))
                draft.DefinitionId = definitionId;
        }

        // Update the draft with the received model.
        var root = model.Root;
        var stringData = JsonSerializer.Serialize(root, serializerOptions);

        draft!.StringData = stringData;
        draft.MaterializerName = JsonWorkflowMaterializer.MaterializerName;
        draft.Name = model.Name?.Trim();
        draft.Description = model.Description?.Trim();
        draft.Metadata = model.Metadata;
        draft.Tags = model.Tags;
        draft.Variables = model.Variables;
        draft.ApplicationProperties = model.ApplicationProperties;
        draft = publish ? await _workflowPublisher.PublishAsync(draft, cancellationToken) : await _workflowPublisher.SaveDraftAsync(draft, cancellationToken);

        var result = Json(draft, serializerOptions);
        result.StatusCode = isNew ? StatusCodes.Status201Created : StatusCodes.Status200OK;

        return result;
    }
}