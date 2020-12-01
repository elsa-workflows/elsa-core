using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Samples.CustomActivityTypeProviders.Activities;
using Elsa.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Samples.CustomActivityTypeProviders.Endpoints
{
    [ApiController]
    [Route("wakeup")]
    public class WakeUp : Controller
    {
        private readonly IWorkflowInterruptor _workflowInterruptor;
        private readonly IWorkflowInstanceManager _workflowInstanceManager;

        public WakeUp(IWorkflowInterruptor workflowInterruptor, IWorkflowInstanceManager workflowInstanceManager)
        {
            _workflowInterruptor = workflowInterruptor;
            _workflowInstanceManager = workflowInstanceManager;
        }
        
        [HttpGet]
        public async Task<IActionResult> Handle(CancellationToken cancellationToken)
        {
            // Get all workflows blocked on the "Sleep" activity.
            var suspendedWorkflows = (await _workflowInstanceManager.ListByBlockingActivityTypeAsync(nameof(Sleep), cancellationToken)).ToList();

            // Interrupt each workflow by triggering the "Sleep" activity.
            foreach (var workflowInstance in suspendedWorkflows) 
                await _workflowInterruptor.InterruptActivityTypeAsync(workflowInstance, nameof(Sleep), cancellationToken: cancellationToken);

            return Ok($"Interrupted {suspendedWorkflows.Count} workflows.");
        } 
    }
}