using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Dashboard.Options;
using Elsa.Dashboard.Services;
using Elsa.Models;
using Elsa.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Elsa.Dashboard.Areas.Elsa.Controllers
{
    [Area("Elsa")]
    [Route("[area]/[controller]")]
    public class TestController : Controller
    {
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;
        private readonly IOptions<ElsaDashboardOptions> options;
        private readonly INotifier notifier;
        public TestController(
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowDefinitionStore workflowDefinitionStore, 
            IOptions<ElsaDashboardOptions> options, 
            INotifier notifier)
        {
            this.workflowInstanceStore = workflowInstanceStore;
            this.workflowDefinitionStore = workflowDefinitionStore;
            this.options = options;
            this.notifier = notifier;
        }

        [HttpGet("queryUserTask/{activityType}/{tag}")]
        public async Task<IActionResult> QueryUserTask(string activityType, string tag, CancellationToken cancellationToken)
        {
            var tuples = await workflowInstanceStore.ListByBlockingActivityTagAsync(activityType, tag);
            List<BlockingActivity> pendingUserTasks = new List<BlockingActivity>();

            foreach(var item in tuples)
            {
                if (item.BlockingActivity != null)
                    pendingUserTasks.Add(item.BlockingActivity);
            }

            return Ok(pendingUserTasks);
        }
    }
}
