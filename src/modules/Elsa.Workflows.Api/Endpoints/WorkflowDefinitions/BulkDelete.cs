using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore.Attributes;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "BulkDelete")]
public class BulkDelete : Controller
{
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;

    public BulkDelete(IWorkflowDefinitionManager workflowDefinitionManager, WorkflowSerializerOptionsProvider serializerOptionsProvider)
    {
        _workflowDefinitionManager = workflowDefinitionManager;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    [HttpPost]
    public async Task<IActionResult> HandleAsync(CancellationToken cancellationToken)
    {
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var model = await Request.ReadFromJsonAsync<BulkDeleteWorkflowDefinitionsRequest>(serializerOptions, cancellationToken);
        var count = await _workflowDefinitionManager.BulkDeleteByDefinitionIdsAsync(model!.DefinitionIds, cancellationToken);

        return Json(new BulkDeleteWorkflowDefinitionsResponse(count), serializerOptions);
    }

    public record BulkDeleteWorkflowDefinitionsRequest(ICollection<string> DefinitionIds);

    public record BulkDeleteWorkflowDefinitionsResponse([property: JsonPropertyName("deleted")] int DeletedCount);
}