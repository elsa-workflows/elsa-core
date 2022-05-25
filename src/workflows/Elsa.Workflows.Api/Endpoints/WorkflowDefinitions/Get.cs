using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore;
using Elsa.Persistence.Models;
using Elsa.Persistence.Services;
using Elsa.Serialization;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Runtime.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "Get")]
[ProducesResponseType(typeof(WorkflowDefinitionModel), StatusCodes.Status200OK)]
public class Get : Controller
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;

    public Get(IWorkflowDefinitionStore store, IWorkflowDefinitionService workflowDefinitionService, WorkflowSerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _workflowDefinitionService = workflowDefinitionService;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    [HttpGet]
    public async Task<IActionResult> HandleAsync(
        CancellationToken cancellationToken,
        [FromRoute] string definitionId,
        [FromQuery] string? versionOptions = default)
    {
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var parsedVersionOptions = versionOptions != null ? VersionOptions.FromString(versionOptions) : VersionOptions.Latest;
        var definition = await _store.FindByDefinitionIdAsync(definitionId, parsedVersionOptions, cancellationToken);

        if (definition == null)
            return NotFound();

        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);

        var model = new WorkflowDefinitionModel(
            definition.Id,
            definition.DefinitionId,
            definition.Name,
            definition.Description,
            definition.CreatedAt,
            definition.Version,
            definition.Variables,
            definition.Metadata,
            definition.ApplicationProperties,
            definition.IsLatest,
            definition.IsPublished,
            definition.Tags,
            workflow.Root);

        return Json(model, serializerOptions);
    }
}