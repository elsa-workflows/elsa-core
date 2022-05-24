using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore;
using Elsa.Persistence.Services;
using Elsa.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowInstances, "Get")]
public class Get : Controller
{
    private readonly IWorkflowInstanceStore _store;
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;

    public Get(IWorkflowInstanceStore store, WorkflowSerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
    }
    
    [HttpGet]
    public async Task<IActionResult> HandleAsync(string id, CancellationToken cancellationToken)
    {
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var workflowInstance = await _store.FindByIdAsync(id, cancellationToken);
        return workflowInstance != null ? Json(workflowInstance, serializerOptions) : NotFound();
    }
}