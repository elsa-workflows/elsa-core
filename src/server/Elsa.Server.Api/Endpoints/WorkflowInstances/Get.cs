using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Repositories;
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
        private readonly IWorkflowInstanceRepository _workflowInstanceManager;

        public Get(IWorkflowInstanceRepository workflowInstanceRepository)
        {
            _workflowInstanceManager = workflowInstanceRepository;
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
            var workflowInstance = await _workflowInstanceManager.GetByIdAsync(id, cancellationToken);
            return workflowInstance ?? (ActionResult<WorkflowInstance>) NotFound();
        }
    }
}