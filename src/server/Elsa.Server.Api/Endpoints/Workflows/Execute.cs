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
    [Route("v{apiVersion:apiVersion}/workflows/{workflowDefinitionId}/execute")]
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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExecuteWorkflowDefinitionResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Executes the specified workflow definition.",
            Description = "Executes the specified workflow definition.",
            OperationId = "WorkflowDefinitions.Execute",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<IActionResult> Handle(string workflowDefinitionId, ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
        {
            var startableWorkflow = await _workflowLaunchpad.CollectStartableWorkflowAsync(workflowDefinitionId, request.ActivityId, request.CorrelationId, request.ContextId, default, cancellationToken);

            if (startableWorkflow == null)
                return NotFound();

            var result = await _workflowLaunchpad.ExecuteStartableWorkflowAsync(startableWorkflow, request.Input, cancellationToken);

            if (Response.HasStarted)
                return new EmptyResult();

            return Ok(new ExecuteWorkflowDefinitionResponse(
                result.Executed,
                result.ActivityId,
                result.WorkflowInstance
            ));
        }
    }
}