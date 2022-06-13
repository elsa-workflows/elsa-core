using System.Threading.Tasks;
using Elsa.Activities.Signaling.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.Http.Endpoints.Signals
{
    [ApiController]
    [Route("signals/dispatch/{token}")]
    [Produces("application/json")]
    [Authorize]
    public class DispatchEndpoint : ControllerBase
    {
        private readonly ISignaler _signaler;

        public DispatchEndpoint(ISignaler signaler)
        {
            _signaler = signaler;
        }

        [HttpGet, HttpPost]
        public async Task<IActionResult> Handle(string token)
        {
            var pendingWorkflows = await _signaler.DispatchSignalTokenAsync(token).ToList();
            return Accepted(pendingWorkflows);
        }
    }
}