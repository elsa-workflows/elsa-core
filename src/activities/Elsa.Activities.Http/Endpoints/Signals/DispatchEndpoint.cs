using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Signaling.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Activities.Http.Endpoints.Signals
{
    [ApiController]
    [Route("signals/dispatch/{token}")]
    [Produces("application/json")]
    public class DispatchEndpoint : ControllerBase
    {
        private readonly ISignaler _signaler;

        public DispatchEndpoint(ISignaler signaler)
        {
            _signaler = signaler;
        }

        [HttpGet, HttpPost]
        public async Task<IActionResult> Handle(string token, CancellationToken cancellationToken)
        {
            if (!await _signaler.DispatchSignalTokenAsync(token, cancellationToken: cancellationToken))
                return NotFound();
            
            return Accepted();
        }
    }
}