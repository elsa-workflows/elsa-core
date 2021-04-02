using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Activities.Signaling.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Activities.Http.Endpoints.Signals
{
    [ApiController]
    [Route("signals/dispatch/{token}")]
    [Produces("application/json")]
    public class DispatchEndpoint : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly ISignaler _signaler;

        public DispatchEndpoint(ITokenService tokenService, ISignaler signaler)
        {
            _tokenService = tokenService;
            _signaler = signaler;
        }

        [HttpGet, HttpPost]
        public async Task<IActionResult> Handle(string token, CancellationToken cancellationToken)
        {
            if (!_tokenService.TryDecryptToken(token, out Signal signal))
                return NotFound();

            await _signaler.DispatchSignalAsync(signal.Name, null, signal.WorkflowInstanceId, cancellationToken);
            return Accepted();
        }
    }
}