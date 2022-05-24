using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore;
using Elsa.Persistence.Services;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Api.Endpoints.WorkflowInstances;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowInstances, "BulkCancel")]
public class BulkCancel : Controller
{
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;

    public BulkCancel(WorkflowSerializerOptionsProvider serializerOptionsProvider)
    {
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    [HttpPost]
    public async Task<IActionResult> HandleAsync(CancellationToken cancellationToken)
    {
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var model = await Request.ReadFromJsonAsync<BulkCancelWorkflowInstancesRequest>(serializerOptions, cancellationToken);
        // TODO: Implement workflow cancellation.
        var count = -1;

        return Json(new BulkCancelWorkflowInstancesResponse(count), serializerOptions);
    }

    public record BulkCancelWorkflowInstancesRequest(ICollection<string> Ids);

    public record BulkCancelWorkflowInstancesResponse([property: JsonPropertyName("cancelled")] int CancelledCount);
}