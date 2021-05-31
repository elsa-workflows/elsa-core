using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Signaling.Services;
using Elsa.Server.Api.ActionFilters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Open.Linq.AsyncExtensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.Signals
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/signals/{signalName}/dispatch")]
    [Produces("application/json")]
    public class Dispatch : Controller
    {
        private readonly ISignaler _signaler;

        public Dispatch(ISignaler signaler)
        {
            _signaler = signaler;
        }

        [HttpPost]
        [ElsaJsonFormatter]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DispatchSignalResponse))]
        [SwaggerOperation(
            Summary = "Signals all workflows waiting on the specified signal name synchronously.",
            Description = "Signals all workflows waiting on the specified signal name synchronously.",
            OperationId = "Signals.Execute",
            Tags = new[] { "Signals" })
        ]
        public async Task<IActionResult> Handle(string signalName, DispatchSignalRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _signaler.DispatchSignalAsync(signalName, request.Input, request.WorkflowInstanceId, request.CorrelationId, cancellationToken).ToList();

            if (Response.HasStarted)
                return new EmptyResult();

            return Ok(new DispatchSignalResponse(result));
        }
    }
}