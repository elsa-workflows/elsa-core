using System.Threading.Tasks;
using Elsa.Activities.Conductor.Models;
using Elsa.Activities.Conductor.Providers.Bookmarks;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Activities.Conductor.Endpoints.Conductor.Tasks
{
    [ApiController]
    [Route("conductor/tasks/{taskName}/dispatch")]
    [Produces("application/json")]
    public class Dispatch : ControllerBase
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        public Dispatch(IWorkflowLaunchpad workflowLaunchpad) => _workflowLaunchpad = workflowLaunchpad;

        [HttpGet, HttpPost]
        public async Task<IActionResult> Handle(string taskName, TaskResultModel model)
        {
            var bookmark = new TaskBookmark(taskName.ToLowerInvariant());
            var context = new WorkflowsQuery(nameof(RunTask), bookmark, model.CorrelationId, model.WorkflowInstanceId);
            var pendingWorkflows = await _workflowLaunchpad.CollectAndDispatchWorkflowsAsync(context, new WorkflowInput(model));
            
            return Accepted(pendingWorkflows);
        }
    }
}