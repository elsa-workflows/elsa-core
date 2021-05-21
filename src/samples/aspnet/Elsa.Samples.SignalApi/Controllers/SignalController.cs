using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Signaling.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Samples.SignalApi.Controllers
{
    [ApiController]
    [Route("fire")]
    public class FireController : Controller
    {
        private readonly ISignaler _signaler;

        public FireController(ISignaler signaler)
        {
            _signaler = signaler;
        }
        
        [HttpGet("start")]
        public async Task<IActionResult> StartFire(CancellationToken cancellationToken)
        {
            var startedWorkflows = await _signaler.TriggerSignalAsync("Fire", cancellationToken: cancellationToken);
            return Ok(startedWorkflows);
        }
        
        [HttpGet("{workflowInstanceId}/extinguish")]
        public async Task<IActionResult> Extinguish(string workflowInstanceId, CancellationToken cancellationToken)
        {
            var startedWorkflows = await _signaler.TriggerSignalAsync("Water", workflowInstanceId: workflowInstanceId, cancellationToken: cancellationToken);
            return Ok(startedWorkflows);
        }
    }
}