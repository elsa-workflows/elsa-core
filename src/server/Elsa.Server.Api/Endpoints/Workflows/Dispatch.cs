using System.Threading;
using System.Threading.Tasks;
using Elsa.Server.Api.ActionFilters;
using Elsa.Server.Api.Endpoints.WorkflowDefinitions;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.Workflows
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflows/{workflowDefinitionId}/dispatch")]
    [Produces("application/json")]
    public class Dispatch : Controller
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;

        public Dispatch(IWorkflowLaunchpad workflowLaunchpad)
        {
            _workflowLaunchpad = workflowLaunchpad;
        }

        [HttpPost]
        [ElsaJsonFormatter]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DispatchWorkflowDefinitionResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Executes the specified workflow definition.",
            Description = "Executes the specified workflow definition.",
            OperationId = "Workflows.Dispatch",
            Tags = new[] { "Workflows" })
        ]
        public async Task<IActionResult> Handle(string workflowDefinitionId, DispatchWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
        {
            var startableWorkflow = await _workflowLaunchpad.CollectStartableWorkflowAsync(workflowDefinitionId, request.ActivityId, request.CorrelationId, request.ContextId, default, cancellationToken);

            if (startableWorkflow == null)
                return NotFound();
            
            var result = await _workflowLaunchpad.DispatchStartableWorkflowAsync(startableWorkflow, request.Input, cancellationToken);
            return Ok(new DispatchWorkflowDefinitionResponse(result.WorkflowInstanceId, result.ActivityId));
        }
    }
}