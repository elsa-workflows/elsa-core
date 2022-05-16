using System.Threading;
using System.Threading.Tasks;
using Elsa.Api.ApiResults;
using Elsa.AspNetCore;
using Elsa.Persistence.Models;
using Elsa.Persistence.Services;
using Elsa.Runtime.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Api.Endpoints.WorkflowDefinitions;

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