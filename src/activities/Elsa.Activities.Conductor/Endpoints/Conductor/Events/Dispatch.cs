using System.Threading.Tasks;
using Elsa.Activities.Conductor.Models;
using Elsa.Activities.Conductor.Providers.Bookmarks;
using Elsa.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Activities.Conductor.Endpoints.Conductor.Events
{
    [ApiController]
    [Route("conductor/events/{eventName}/dispatch")]
    [Produces("application/json")]
    public class Dispatch : ControllerBase
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        public Dispatch(IWorkflowLaunchpad workflowLaunchpad) => _workflowLaunchpad = workflowLaunchpad;

        [HttpGet, HttpPost]
        public async Task<IActionResult> Handle(string eventName, EventModel model)
        {
            var bookmark = new EventBookmark(eventName.ToLowerInvariant());
            var trigger = new EventBookmark(eventName.ToLowerInvariant());
            var context = new CollectWorkflowsContext(nameof(EventReceived), bookmark, trigger, model.CorrelationId, model.WorkflowInstanceId);
            var pendingWorkflows = await _workflowLaunchpad.CollectAndDispatchWorkflowsAsync(context, model);
            
            return Accepted(pendingWorkflows);
        }
    }
}