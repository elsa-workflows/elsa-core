using System.Threading;
using System.Threading.Tasks;
using Elsa.Server.Api.ActionFilters;
using Elsa.Server.Api.Endpoints.WorkflowDefinitions;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowInstances
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-instances/{workflowInstanceId}/execute")]
    [Produces("application/json")]
    public class Execute : Controller
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;

        public Execute(IWorkflowLaunchpad workflowLaunchpad)
        {
            _workflowLaunchpad = workflowLaunchpad;
        }

        [HttpPost]
        [ElsaJsonFormatter]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExecuteWorkflowInstanceResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Executes the specified workflow instance.",
            Description = "Executes the specified workflow instance.",
            OperationId = "WorkflowInstances.Execute",
            Tags = new[] { "WorkflowInstances" })
        ]
        public async Task<IActionResult> Handle(string workflowInstanceId, ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _workflowLaunchpad.ExecutePendingWorkflowAsync(workflowInstanceId, request.ActivityId, request.Input, cancellationToken);

            if (Response.HasStarted)
                return new EmptyResult();

            return Ok(new ExecuteWorkflowInstanceResponse(result.Executed, result.ActivityId, result.WorkflowInstance));
        }
    }
}