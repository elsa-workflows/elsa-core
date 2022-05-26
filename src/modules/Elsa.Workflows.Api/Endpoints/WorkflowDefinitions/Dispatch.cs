using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore;
using Elsa.Workflows.Api.ApiResults;
using Elsa.Workflows.Persistence.Models;
using Elsa.Workflows.Persistence.Services;
using Elsa.Workflows.Runtime.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "Dispatch")]
[ProducesResponseType(typeof(DispatchWorkflowDefinitionResponse), StatusCodes.Status200OK)]
public class Dispatch : Controller
{
    private readonly IWorkflowDefinitionStore _store;
    public Dispatch(IWorkflowDefinitionStore store) => _store = store;

    [HttpPost]
    public async Task<IActionResult> DispatchAsync(string definitionId, string? correlationId = default, CancellationToken cancellationToken = default)
    {
        var exists = await _store.GetExistsAsync(definitionId, VersionOptions.Published, cancellationToken);
        return exists ? new DispatchWorkflowDefinitionResult(definitionId, correlationId) : NotFound();
    }
}