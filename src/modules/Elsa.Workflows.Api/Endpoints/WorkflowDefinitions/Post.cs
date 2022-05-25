using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions;

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
        ICollection<Variable> Variables,
        IDictionary<string, object> Metadata,
        IDictionary<string, object> ApplicationProperties,
        ICollection<string> Tags,
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
        if (isNew)
        {
            draft = _workflowPublisher.New();

            if (!string.IsNullOrWhiteSpace(definitionId))
                draft.DefinitionId = definitionId;
        }

        // Update the draft with the received model.
        var root = model.Root ?? new Sequence();
        var stringData = JsonSerializer.Serialize(root, serializerOptions);

        draft!.StringData = stringData;
        draft.MaterializerName = JsonWorkflowMaterializer.MaterializerName;
        draft.Name = model.Name?.Trim();
        draft.Description = model.Description?.Trim();
        draft.Metadata = model.Metadata;
        draft.Tags = model.Tags;
        draft.Variables = model.Variables;
        draft.ApplicationProperties = model.ApplicationProperties;
        draft = model.Publish ? await _workflowPublisher.PublishAsync(draft, cancellationToken) : await _workflowPublisher.SaveDraftAsync(draft, cancellationToken);

        var result = Json(draft, serializerOptions);
        result.StatusCode = isNew ? StatusCodes.Status201Created : StatusCodes.Status200OK;

        return result;
    }
}