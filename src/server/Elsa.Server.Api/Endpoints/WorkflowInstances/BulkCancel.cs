using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowInstances
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-instances/bulk/cancel")]
    [Produces("application/json")]
    public class BulkCancel : Controller
    {
        private readonly IWorkflowInstanceCanceller _workflowInstanceCanceller;

        public BulkCancel(IWorkflowInstanceCanceller workflowInstanceCanceller)
        {
            _workflowInstanceCanceller = workflowInstanceCanceller;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [SwaggerOperation(
            Summary = "Cancels a set of workflow instances.",
            Description = "Cancels a set of workflow instances.",
            OperationId = "WorkflowInstances.BulkCancel",
            Tags = new[] { "WorkflowInstances" })
        ]
        public async Task<IActionResult> Handle(BulkCancelWorkflowsRequest request, CancellationToken cancellationToken = default)
        {
            var tasks = request.WorkflowInstanceIds.Select(x => _workflowInstanceCanceller.CancelAsync(x, cancellationToken));
            var results = await Task.WhenAll(tasks);
            var count = results.Where(x => x.Status == CancelWorkflowInstanceResultStatus.Ok);
            
            return Ok(new
            {
                CancelledWorkflowCount = count
            });
        }
    }
}