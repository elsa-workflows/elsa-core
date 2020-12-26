using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowInstances
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-instances/{id}")]
    [Produces("application/json")]
    public class Delete : Controller
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;

        public Delete(IWorkflowInstanceStore workflowInstanceStore)
        {
            _workflowInstanceStore = workflowInstanceStore;
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Deletes a workflow instance.",
            Description = "Deletes a workflow instance.",
            OperationId = "WorkflowInstances.Delete",
            Tags = new[] { "WorkflowInstances" })
        ]
        public async Task<IActionResult> Handle(string id, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowInstanceStore.FindByIdAsync(id, cancellationToken);

            if (workflowInstance == null)
                return NotFound();
            
            await _workflowInstanceStore.DeleteAsync(workflowInstance, cancellationToken);
            return NoContent();
        }
    }
}