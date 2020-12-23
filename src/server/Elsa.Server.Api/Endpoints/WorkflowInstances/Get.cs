using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
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
    public class Get : Controller
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;

        public Get(IWorkflowInstanceStore workflowInstanceStore)
        {
            _workflowInstanceStore = workflowInstanceStore;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkflowInstance))]
        [SwaggerOperation(
            Summary = "Returns a single workflow instance.",
            Description = "Returns a single workflow instance.",
            OperationId = "WorkflowInstances.Get",
            Tags = new[] { "WorkflowInstances" })
        ]
        public async Task<ActionResult<WorkflowInstance>> Handle(string id, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowInstanceStore.FindByIdAsync(id, cancellationToken);
            return workflowInstance ?? (ActionResult<WorkflowInstance>) NotFound();
        }
    }
}