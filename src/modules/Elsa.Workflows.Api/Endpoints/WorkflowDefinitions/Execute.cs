using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore;
using Elsa.AspNetCore.Attributes;
using Elsa.Workflows.Api.ApiResults;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Persistence.Models;
using Elsa.Workflows.Persistence.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "Execute")]
[ProducesResponseType(typeof(InvokeWorkflowResult), StatusCodes.Status200OK)]
public class Execute : Controller
{
    private readonly IWorkflowDefinitionStore _store;
    public Execute(IWorkflowDefinitionStore store) => _store = store;

    [HttpPost]
    public async Task<IActionResult> ExecuteAsync(string definitionId, string? correlationId = default, CancellationToken cancellationToken = default)
    {
        var exists = await _store.GetExistsAsync(definitionId, VersionOptions.Published, cancellationToken);
        return exists ? new ExecuteWorkflowDefinitionResult(definitionId, correlationId) : NotFound();
    }
}