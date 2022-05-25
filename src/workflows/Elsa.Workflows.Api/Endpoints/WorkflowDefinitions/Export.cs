using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Core.Serialization;
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
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;

    public Export(IWorkflowDefinitionStore store, IWorkflowDefinitionService workflowDefinitionService, WorkflowSerializerOptionsProvider serializerOptionsProvider)
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
        
        var binaryJson = JsonSerializer.SerializeToUtf8Bytes(model, serializerOptions);
        var hasWorkflowName = !string.IsNullOrWhiteSpace(definition.Name);
        var workflowName = hasWorkflowName ? definition.Name!.Trim() : definition.DefinitionId;
            
        var fileName = hasWorkflowName
            ? $"{workflowName.Underscore().Dasherize().ToLowerInvariant()}.json"
            : $"workflow-definition-{workflowName.Underscore().Dasherize().ToLowerInvariant()}.json";

        return File(binaryJson, MediaTypeNames.Application.Json, fileName);
    }
}