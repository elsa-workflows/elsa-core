using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore.Attributes;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Persistence.Models;
using Elsa.Workflows.Persistence.Services;
using Elsa.Workflows.Runtime.Services;
using Humanizer;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "Export")]
[Produces("application/json")]
public class Export : Controller
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private readonly VariableDefinitionMapper _variableDefinitionMapper;

    public Export(
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
        
        var binaryJson = JsonSerializer.SerializeToUtf8Bytes(model, serializerOptions);
        var hasWorkflowName = !string.IsNullOrWhiteSpace(definition.Name);
        var workflowName = hasWorkflowName ? definition.Name!.Trim() : definition.DefinitionId;
            
        var fileName = hasWorkflowName
            ? $"{workflowName.Underscore().Dasherize().ToLowerInvariant()}.json"
            : $"workflow-definition-{workflowName.Underscore().Dasherize().ToLowerInvariant()}.json";

        return File(binaryJson, MediaTypeNames.Application.Json, fileName);
    }
}