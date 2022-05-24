using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore;
using Elsa.Workflows.Management.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;
using Elsa.Persistence.Services;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "Publish")]
[ProducesResponseType(typeof(WorkflowDefinition), StatusCodes.Status200OK)]
public class Publish : Controller
{
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowPublisher _workflowPublisher;

    public Publish(WorkflowSerializerOptionsProvider serializerOptionsProvider, IWorkflowDefinitionStore store, IWorkflowPublisher workflowPublisher)
    {
        _serializerOptionsProvider = serializerOptionsProvider;
        _store = store;
        _workflowPublisher = workflowPublisher;
    }

    public async Task<IActionResult> HandleAsync(string definitionId, CancellationToken cancellationToken)
    {
        var definition = await _store.FindByDefinitionIdAsync(definitionId, VersionOptions.Latest, cancellationToken);

        if (definition == null)
            return NotFound();

        if (definition.IsPublished)
            return BadRequest(new
            {
                Message = $"Workflow with id {definitionId} is already published"
            });

        await _workflowPublisher.PublishAsync(definition, cancellationToken);
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();

        return Json(definition, serializerOptions);
    }
}