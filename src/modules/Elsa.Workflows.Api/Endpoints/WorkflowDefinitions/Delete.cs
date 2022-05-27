using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore;
using Elsa.AspNetCore.Attributes;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Persistence.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "Delete")]
public class Delete : Controller
{
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
    public Delete(IWorkflowDefinitionManager workflowDefinitionManager)
    {
        _workflowDefinitionManager = workflowDefinitionManager;
    }

    [HttpDelete]
    public async Task<IActionResult> HandleAsync(string definitionId, CancellationToken cancellationToken)
    {
        var result = await _workflowDefinitionManager.DeleteByDefinitionIdAsync(definitionId, cancellationToken);
        return result == 0 ? NotFound() : NoContent();
    }
}