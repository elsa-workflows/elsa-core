using System.Threading.Tasks;
using Elsa.Activities.Signaling.Services;
using Microsoft.AspNetCore.Mvc;
using Open.Linq.AsyncExtensions;

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
        public async Task<IActionResult> Handle(string token)
        {
            var result = await _signaler.TriggerSignalTokenAsync(token).ToList();

            return HttpContext.Response.HasStarted
                ? new EmptyResult()
                : Ok(result);
        }
    }
}