using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Samples.Interrupts.Activities;
using Elsa.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Samples.Interrupts.Endpoints
{
    [ApiController]
    [Route("wakeup")]
    public class WakeUp : Controller
    {
        private readonly IWorkflowTriggerInterruptor _workflowInterruptor;

        public WakeUp(IWorkflowTriggerInterruptor workflowInterruptor)
        {
            _workflowInterruptor = workflowInterruptor;
        }
        
        [HttpGet]
        public async Task<IActionResult> Handle(CancellationToken cancellationToken)
        {
            // Interrupt each workflow by triggering the "Sleep" activity.
            var workflowInstances = (await _workflowInterruptor.InterruptActivityTypeAsync(nameof(Sleep), cancellationToken: cancellationToken)).ToList();
            return Ok($"Interrupted {workflowInstances.Count} workflows.");
        } 
    }
}