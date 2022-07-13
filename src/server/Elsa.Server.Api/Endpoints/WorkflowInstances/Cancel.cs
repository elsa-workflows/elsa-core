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
    [Route("v{apiVersion:apiVersion}/workflow-instances/{id}/cancel")]
    [Produces("application/json")]
    public class Cancel : Controller
    {
        private readonly IWorkflowInstanceCanceller _canceller;

        public Cancel(IWorkflowInstanceCanceller canceller)
        {
            _canceller = canceller;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(
            Summary = "Cancels a workflow instance.",
            Description = "Retries a workflow instance.",
            OperationId = "WorkflowInstances.Cancel",
            Tags = new[] { "WorkflowInstances" })
        ]
        public async Task<IActionResult> Handle(string id, CancellationToken cancellationToken = default)
        {
            var result = await _canceller.CancelAsync(id, cancellationToken);

            return result.Status switch
            {
                CancelWorkflowInstanceResultStatus.NotFound => NotFound(),
                CancelWorkflowInstanceResultStatus.InvalidStatus => BadRequest($"Cannot cancel a workflow instance with status {result.WorkflowInstance!.WorkflowStatus}"),
                _ => Ok()
            };
        }
    }
}