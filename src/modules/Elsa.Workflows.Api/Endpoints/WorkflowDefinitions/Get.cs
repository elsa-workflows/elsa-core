using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore.Attributes;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Persistence.Services;
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
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private readonly VariableDefinitionMapper _variableDefinitionMapper;

    public Get(
        IWorkflowDefinitionStore store, 
        IWorkflowDefinitionService workflowDefinitionService, 
        SerializerOptionsProvider serializerOptionsProvider,
        VariableDefinitionMapper variableDefinitionMapper)
    {
        _store = store;
        _workflowDefinitionService = workflowDefinitionService;
        _serializerOptionsProvider = serializerOptionsProvider;
        _variableDefinitionMapper = variableDefinitionMapper;
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
        var variables = _variableDefinitionMapper.Map(workflow.Variables).ToList();

        var model = new WorkflowDefinitionModel(
            definition.Id,
            definition.DefinitionId,
            definition.Name,
            definition.Description,
            definition.CreatedAt,
            definition.Version,
            variables,
            definition.Metadata,
            definition.ApplicationProperties,
            definition.IsLatest,
            definition.IsPublished,
            workflow.Root);

        return Json(model, serializerOptions);
    }
}