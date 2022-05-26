using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Persistence.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "BulkDelete")]
public class BulkDelete : Controller
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;

    public BulkDelete(IWorkflowDefinitionStore store, WorkflowSerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    [HttpPost]
    public async Task<IActionResult> HandleAsync(CancellationToken cancellationToken)
    {
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var model = await Request.ReadFromJsonAsync<BulkDeleteWorkflowDefinitionsRequest>(serializerOptions, cancellationToken);
        var count = await _store.DeleteManyByDefinitionIdsAsync(model!.DefinitionIds, cancellationToken);

        return Json(new BulkDeleteWorkflowDefinitionsResponse(count), serializerOptions);
    }

    public record BulkDeleteWorkflowDefinitionsRequest(ICollection<string> DefinitionIds);

    public record BulkDeleteWorkflowDefinitionsResponse([property: JsonPropertyName("deleted")] int DeletedCount);
}