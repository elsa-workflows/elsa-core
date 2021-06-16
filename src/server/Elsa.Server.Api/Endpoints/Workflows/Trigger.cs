using System.Collections.Generic;
using System.Linq;
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
    [Route("v{apiVersion:apiVersion}/workflows/trigger")]
    [Produces("application/json")]
    public class Trigger : Controller
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        public Trigger(IWorkflowLaunchpad workflowLaunchpad) => _workflowLaunchpad = workflowLaunchpad;

        [HttpPost]
        [ElsaJsonFormatter]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TriggerWorkflowsResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Triggers all workflows matching the specified criteria synchronously.",
            Description = "Triggers all workflows matching the specified criteria synchronously.",
            OperationId = "Workflows.Execute",
            Tags = new[] { "Workflows" })
        ]
        public async Task<IActionResult> Handle(TriggerWorkflowsRequest request, CancellationToken cancellationToken = default)
        {
            var context = new CollectWorkflowsContext(request.ActivityType, request.Bookmark, request.Trigger, request.CorrelationId, request.WorkflowInstanceId, request.ContextId);
            ICollection<TriggeredWorkflow> triggeredWorkflows;

            if (request.Dispatch)
            {
                var result = await _workflowLaunchpad.CollectAndDispatchWorkflowsAsync(context, request.Input, cancellationToken).ToList();
                triggeredWorkflows = result.Select(x => new TriggeredWorkflow(x.WorkflowInstanceId, x.ActivityId)).ToList();
            }
            else
            {
                var result = await _workflowLaunchpad.CollectAndExecuteWorkflowsAsync(context, request.Input, cancellationToken).ToList();
                triggeredWorkflows = result.Select(x => new TriggeredWorkflow(x.WorkflowInstanceId, x.ActivityId)).ToList();
            }
            
            if (Response.HasStarted)
                return new EmptyResult();

            return Ok(new TriggerWorkflowsResponse(triggeredWorkflows));
        }
    }
}