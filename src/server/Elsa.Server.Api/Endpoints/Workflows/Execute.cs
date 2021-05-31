using System.Threading;
using System.Threading.Tasks;
using Elsa.Server.Api.ActionFilters;
using Elsa.Server.Api.Endpoints.WorkflowDefinitions;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Open.Linq.AsyncExtensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.Workflows
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflows/execute")]
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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExecuteWorkflowsResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Triggers all workflows matching the specified criteria synchronously.",
            Description = "Triggers all workflows matching the specified criteria synchronously.",
            OperationId = "Workflows.Execute",
            Tags = new[] { "Workflows" })
        ]
        public async Task<IActionResult> Handle(ExecuteWorkflowsRequest request, CancellationToken cancellationToken = default)
        {
            var context = new CollectWorkflowsContext(request.ActivityType, request.Bookmark, request.Trigger, request.CorrelationId, request.WorkflowInstanceId, request.ContextId);
            var result = await _workflowLaunchpad.CollectAndExecuteWorkflowsAsync(context, request.Input, cancellationToken).ToList();

            if (Response.HasStarted)
                return new EmptyResult();

            return Ok(new ExecuteWorkflowsResponse(result));
        }
    }
}