using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Server.Api.ActionFilters;
using Elsa.Services;
using Elsa.Services.Models;
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
        private readonly ITenantAccessor _tenantAccessor;
        public Trigger(IWorkflowLaunchpad workflowLaunchpad,ITenantAccessor tenantAccessor)
        {
            _workflowLaunchpad = workflowLaunchpad;
            _tenantAccessor = tenantAccessor;
        }

        [HttpPost]
        [ElsaJsonFormatter]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TriggerWorkflowsRequestModel))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Triggers all workflows matching the specified criteria synchronously.",
            Description = "Triggers all workflows matching the specified criteria synchronously.",
            OperationId = "Workflows.Execute",
            Tags = new[] { "Workflows" })
        ]
        public async Task<IActionResult> Handle(TriggerWorkflowsRequestModel request, CancellationToken cancellationToken = default)
        {
            var tenantId = await _tenantAccessor.GetTenantIdAsync(cancellationToken);
            var context = new WorkflowsQuery(request.ActivityType, request.Bookmark, request.CorrelationId, request.WorkflowInstanceId, request.ContextId, tenantId);
            ICollection<TriggeredWorkflowModel> triggeredWorkflows;

            if (request.Dispatch)
            {
                var result = await _workflowLaunchpad.CollectAndDispatchWorkflowsAsync(context, new WorkflowInput(request.Input), cancellationToken).ToList();
                triggeredWorkflows = result.Select(x => new TriggeredWorkflowModel(x.WorkflowInstanceId, x.ActivityId)).ToList();
            }
            else
            {
                var result = await _workflowLaunchpad.CollectAndExecuteWorkflowsAsync(context, new WorkflowInput(request.Input), cancellationToken).ToList();
                triggeredWorkflows = result.Select(x => new TriggeredWorkflowModel(x.WorkflowInstanceId, x.ActivityId)).ToList();
            }
            
            if (Response.HasStarted)
                return new EmptyResult();

            return Ok(new TriggerWorkflowsResponseModel(triggeredWorkflows));
        }
    }
}