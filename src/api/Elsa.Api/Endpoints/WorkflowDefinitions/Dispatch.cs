using System.Threading;
using System.Threading.Tasks;
using Elsa.Api.ApiResults;
using Elsa.AspNetCore;
using Elsa.Persistence.Models;
using Elsa.Runtime.Models;
using Elsa.Runtime.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "Dispatch")]
[ProducesResponseType(typeof(DispatchWorkflowDefinitionResponse), StatusCodes.Status200OK)]
public class Dispatch : Controller
{
    private readonly IWorkflowRegistry _workflowRegistry;
    public Dispatch(IWorkflowRegistry workflowRegistry) => _workflowRegistry = workflowRegistry;

    [HttpPost]
    public async Task<IActionResult> DispatchAsync(string definitionId, string? correlationId = default, CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowRegistry.FindByDefinitionIdAsync(definitionId, VersionOptions.Published, cancellationToken);
        return workflow == null ? NotFound() : new DispatchWorkflowResult(workflow, correlationId);
    }
}