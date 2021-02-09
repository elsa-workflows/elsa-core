using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowInstances
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-instances/bulk")]
    [Produces("application/json")]
    public class BulkDelete : Controller
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;

        public BulkDelete(IWorkflowInstanceStore workflowInstanceStore)
        {
            _workflowInstanceStore = workflowInstanceStore;
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Deletes a workflow instance.",
            Description = "Deletes a workflow instance.",
            OperationId = "WorkflowInstances.BulkDelete",
            Tags = new[] { "WorkflowInstances" })
        ]
        public async Task<IActionResult> Handle(BulkDeleteWorkflowsRequest request, CancellationToken cancellationToken = default)
        {
            var count = await _workflowInstanceStore.DeleteManyAsync(new WorkflowInstanceIdsSpecification(request.WorkflowInstanceIds), cancellationToken);
            return Ok(new
            {
                DeletedWorkflowCount = count
            });
        }
    }
}