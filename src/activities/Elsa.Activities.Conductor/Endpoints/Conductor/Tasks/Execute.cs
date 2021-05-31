using System.Threading.Tasks;
using Elsa.Activities.Conductor.Models;
using Elsa.Activities.Conductor.Providers.Bookmarks;
using Elsa.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Activities.Conductor.Endpoints.Conductor.Tasks
{
    [ApiController]
    [Route("conductor/tasks/{taskName}/execute")]
    [Produces("application/json")]
    public class Execute : ControllerBase
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        public Execute(IWorkflowLaunchpad workflowLaunchpad) => _workflowLaunchpad = workflowLaunchpad;

        [HttpGet, HttpPost]
        public async Task<IActionResult> Handle(string taskName, TaskResultModel model)
        {
            var bookmark = new TaskBookmark(taskName.ToLowerInvariant());
            var trigger = new TaskBookmark(taskName.ToLowerInvariant());
            var context = new CollectWorkflowsContext(nameof(RunTask), bookmark, trigger, model.CorrelationId, model.WorkflowInstanceId);
            var pendingWorkflows = await _workflowLaunchpad.CollectAndExecuteWorkflowsAsync(context, model);
            
            return Accepted(pendingWorkflows);
        }
    }
}