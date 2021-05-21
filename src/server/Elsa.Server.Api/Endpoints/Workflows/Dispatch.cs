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
    [Route("v{apiVersion:apiVersion}/workflows/dispatch")]
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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DispatchWorkflowsResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Triggers all workflows matching the specified criteria asynchronously.",
            Description = "Triggers all workflows matching the specified criteria asynchronously.",
            OperationId = "Workflows.Dispatch",
            Tags = new[] { "Workflows" })
        ]
        public async Task<IActionResult> Handle(DispatchWorkflowsRequest request, CancellationToken cancellationToken = default)
        {
            var context = new CollectWorkflowsContext(request.ActivityType, request.Bookmark, request.Trigger, request.CorrelationId, request.WorkflowInstanceId, request.ContextId);
            var result = await _workflowLaunchpad.CollectAndDispatchWorkflowsAsync(context, request.Input, cancellationToken).ToList();

            if (Response.HasStarted)
                return new EmptyResult();

            return Ok(new DispatchWorkflowsResponse(result));
        }
    }
}