using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Signaling.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Activities.Http.Endpoints.Signals
{
    [ApiController]
    [Route("signals/trigger/{token}")]
    [Produces("application/json")]
    public class TriggerEndpoint : ControllerBase
    {
        private readonly ISignaler _signaler;
        public TriggerEndpoint(ISignaler signaler) => _signaler = signaler;

        [HttpGet, HttpPost]
        public async Task<IActionResult> Handle(string token, CancellationToken cancellationToken)
        {
            if (!await _signaler.TriggerSignalTokenAsync(token, cancellationToken: cancellationToken))
                return NotFound();

            return HttpContext.Response.HasStarted
                ? new EmptyResult()
                : Accepted();
        }
    }
}