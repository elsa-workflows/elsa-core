using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore.Attributes;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Persistence.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowInstances, "BulkDelete")]
public class BulkDelete : Controller
{
    private readonly IWorkflowInstanceStore _store;
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;

    public BulkDelete(IWorkflowInstanceStore store, WorkflowSerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    [HttpPost]
    public async Task<IActionResult> HandleAsync(CancellationToken cancellationToken)
    {
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var model = await Request.ReadFromJsonAsync<BulkDeleteWorkflowInstancesRequest>(serializerOptions, cancellationToken);
        var count = await _store.DeleteManyAsync(model!.Ids, cancellationToken);

        return Json(new BulkDeleteWorkflowInstancesResponse(count), serializerOptions);
    }

    public record BulkDeleteWorkflowInstancesRequest(ICollection<string> Ids);

    public record BulkDeleteWorkflowInstancesResponse([property: JsonPropertyName("deleted")] int DeletedCount);
}