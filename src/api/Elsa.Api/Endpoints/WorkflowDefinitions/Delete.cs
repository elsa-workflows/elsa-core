using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore;
using Elsa.Persistence.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "Delete")]
public class Delete : Controller
{
    private readonly IWorkflowDefinitionStore _store;
    public Delete(IWorkflowDefinitionStore store) => _store = store;

    [HttpDelete]
    public async Task<IActionResult> HandleAsync(string definitionId, CancellationToken cancellationToken)
    {
        var result = await _store.DeleteByDefinitionIdAsync(definitionId, cancellationToken);
        return result == 0 ? NotFound() : NoContent();
    }
}