using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Events;
using Elsa.Activities.Signaling.Models;
using Elsa.Activities.Signaling.Services;
using Elsa.Services;
using MediatR;
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
        private readonly ITokenService _tokenService;
        private readonly IMediator _mediator;

        public TriggerEndpoint(ISignaler signaler, ITokenService tokenService, IMediator mediator)
        {
            _signaler = signaler;
            _tokenService = tokenService;
            _mediator = mediator;
        }

        [HttpGet, HttpPost]
        public async Task<IActionResult> Handle(string token, CancellationToken cancellationToken)
        {
            if (!_tokenService.TryDecryptToken(token, out SignalModel signal))
                return BadRequest();

            var affectedWorkflows = await _signaler.TriggerSignalAsync(signal.Name, null, signal.WorkflowInstanceId, cancellationToken: cancellationToken).ToList();

            await _mediator.Publish(new HttpTriggeredSignal(signal, affectedWorkflows), cancellationToken);

            var response = HttpContext.Response;

            return response.HasStarted || response.StatusCode != (int) HttpStatusCode.OK
                ? new EmptyResult()
                : Ok(affectedWorkflows);
        }
    }
}