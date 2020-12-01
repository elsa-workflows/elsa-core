using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public WakeUp(IWorkflowInterruptor workflowInterruptor)
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