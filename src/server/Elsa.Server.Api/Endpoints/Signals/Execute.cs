using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Signaling.Services;
using Elsa.Server.Api.ActionFilters;
using Elsa.Server.Api.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Open.Linq.AsyncExtensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.Signals
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/signals/{signalName}/execute")]
    [Produces("application/json")]
    public class Execute : Controller
    {
        private readonly ISignaler _signaler;
        private readonly IEndpointContentSerializerSettingsProvider _serializerSettingsProvider;

        public Execute(ISignaler signaler, IEndpointContentSerializerSettingsProvider serializerSettingsProvider)
        {
            _signaler = signaler;
            _serializerSettingsProvider = serializerSettingsProvider;
        }

        [HttpPost]
        [ElsaJsonFormatter]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExecuteSignalResponse))]
        [SwaggerOperation(
            Summary = "Signals all workflows waiting on the specified signal name synchronously.",
            Description = "Signals all workflows waiting on the specified signal name synchronously.",
            OperationId = "Signals.Execute",
            Tags = new[] { "Signals" })
        ]
        public async Task<IActionResult> Handle(string signalName, ExecuteSignalRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _signaler.TriggerSignalAsync(signalName, request.Input, request.WorkflowInstanceId, request.CorrelationId, cancellationToken).ToList();

            if (Response.HasStarted)
                return new EmptyResult();

            return Json(
                new ExecuteSignalResponse(result.Select(x => new CollectedWorkflow(x.WorkflowInstanceId, x.WorkflowInstance, x.ActivityId)).ToList()),
                _serializerSettingsProvider.GetSettings());
        }
    }
}