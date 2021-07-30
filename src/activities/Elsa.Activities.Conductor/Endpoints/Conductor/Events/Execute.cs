using System.Threading.Tasks;
using Elsa.Activities.Conductor.Models;
using Elsa.Activities.Conductor.Providers.Bookmarks;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Activities.Conductor.Endpoints.Conductor.Events
{
    [ApiController]
    [Route("conductor/events/{eventName}/execute")]
    [Produces("application/json")]
    public class Execute : ControllerBase
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        public Execute(IWorkflowLaunchpad workflowLaunchpad) => _workflowLaunchpad = workflowLaunchpad;

        [HttpGet, HttpPost]
        public async Task<IActionResult> Handle(string eventName, EventModel model)
        {
            var bookmark = new EventBookmark(eventName);
            var context = new WorkflowsQuery(nameof(EventReceived), bookmark, model.CorrelationId, model.WorkflowInstanceId);
            var pendingWorkflows = await _workflowLaunchpad.CollectAndExecuteWorkflowsAsync(context, new WorkflowInput(model));
            
            return Accepted(pendingWorkflows);
        }
    }
}