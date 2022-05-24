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
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "Retract")]
[ProducesResponseType(typeof(WorkflowDefinition), StatusCodes.Status200OK)]
public class Retract : Controller
{
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowPublisher _workflowPublisher;

    public Retract(
        WorkflowSerializerOptionsProvider serializerOptionsProvider,
        IWorkflowDefinitionStore store,
        IWorkflowPublisher workflowPublisher)
    {
        _serializerOptionsProvider = serializerOptionsProvider;
        _store = store;
        _workflowPublisher = workflowPublisher;
    }

    public async Task<IActionResult> HandleAsync(string definitionId, CancellationToken cancellationToken)
    {
        var definition = await _store.FindByDefinitionIdAsync(definitionId, VersionOptions.LatestOrPublished, cancellationToken);

        if (definition == null)
            return NotFound();
        
        if (!definition.IsPublished)
            return BadRequest(new
            {
                Message = $"Workflow with id {definitionId} is not published"
            });

        await _workflowPublisher.RetractAsync(definition, cancellationToken);
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();

        return Json(definition, serializerOptions);
    }
}