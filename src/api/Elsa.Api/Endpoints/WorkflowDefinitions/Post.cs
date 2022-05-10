using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities;
using Elsa.AspNetCore;
using Elsa.Management.Materializers;
using Elsa.Management.Services;
using Elsa.Persistence.Entities;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "Post")]
[ProducesResponseType(typeof(WorkflowDefinition), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(WorkflowDefinition), StatusCodes.Status201Created)]
public class Post : Controller
{
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;
    private readonly IWorkflowPublisher _workflowPublisher;

    public Post(WorkflowSerializerOptionsProvider serializerOptionsProvider, IWorkflowPublisher workflowPublisher)
    {
        _serializerOptionsProvider = serializerOptionsProvider;
        _workflowPublisher = workflowPublisher;
    }

    public record SaveWorkflowDefinitionRequest(
        string? DefinitionId,
        string? Name,
        string? Description,
        IActivity? Root,
        bool Publish);

    [HttpPost]
    public async Task<IActionResult> HandleAsync(CancellationToken cancellationToken)
    {
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var model = (await Request.ReadFromJsonAsync<SaveWorkflowDefinitionRequest>(serializerOptions, cancellationToken))!;
        var definitionId = model.DefinitionId;

        // Get a workflow draft version.
        var draft = !string.IsNullOrWhiteSpace(definitionId)
            ? await _workflowPublisher.GetDraftAsync(definitionId, cancellationToken)
            : default;

        var isNew = draft == null;

        // Create a new workflow in case no existing definition was found.
        if (draft == null)
        {
            draft = _workflowPublisher.New();

            if (!string.IsNullOrWhiteSpace(definitionId))
                draft.DefinitionId = definitionId;
        }

        // Update the draft with the received model.
        var root = model.Root ?? new Sequence();
        var stringData = JsonSerializer.Serialize(root, serializerOptions);

        draft.StringData = stringData;
        draft.MaterializerName = JsonWorkflowMaterializer.MaterializerName;
        draft.Name = model.Name?.Trim();
        draft.Description = model.Description?.Trim();
        draft = model.Publish ? await _workflowPublisher.PublishAsync(draft, cancellationToken) : await _workflowPublisher.SaveDraftAsync(draft, cancellationToken);

        var result = Json(draft, serializerOptions);
        result.StatusCode = isNew ? StatusCodes.Status201Created : StatusCodes.Status200OK;

        return result;
    }
}