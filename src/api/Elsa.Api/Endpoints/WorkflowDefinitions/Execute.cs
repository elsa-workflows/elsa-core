using System.Threading;
using System.Threading.Tasks;
using Elsa.Api.ApiResults;
using Elsa.AspNetCore;
using Elsa.Models;
using Elsa.Persistence.Models;
using Elsa.Runtime.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "Execute")]
[ProducesResponseType(typeof(InvokeWorkflowResult), StatusCodes.Status200OK)]
public class Execute : Controller
{
    private readonly IWorkflowRegistry _workflowRegistry;
    public Execute(IWorkflowRegistry workflowRegistry) => _workflowRegistry = workflowRegistry;

    [HttpPost]
    public async Task<IActionResult> ExecuteAsync(string definitionId, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRegistry.FindByDefinitionIdAsync(definitionId, VersionOptions.Published, cancellationToken);
        return workflow == null ? NotFound() : new ExecuteWorkflowResult(workflow);
    }
}