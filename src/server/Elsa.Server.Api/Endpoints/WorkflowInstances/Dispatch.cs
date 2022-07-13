using System.Threading;
using System.Threading.Tasks;
using Elsa.Server.Api.ActionFilters;
using Elsa.Server.Api.Services;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowInstances
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-instances/{workflowInstanceId}/dispatch")]
    [Produces("application/json")]
    public class Dispatch : Controller
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        private readonly IEndpointContentSerializerSettingsProvider _serializerSettingsProvider;

        public Dispatch(IWorkflowLaunchpad workflowLaunchpad, IEndpointContentSerializerSettingsProvider serializerSettingsProvider)
        {
            _workflowLaunchpad = workflowLaunchpad;
            _serializerSettingsProvider = serializerSettingsProvider;
        }

        [HttpPost]
        [ElsaJsonFormatter]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DispatchWorkflowInstanceResponseModel))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Dispatches the specified workflow instance.",
            Description = "Dispatches the specified workflow instance.",
            OperationId = "WorkflowInstances.Dispatch",
            Tags = new[] { "WorkflowInstances" })
        ]
        public async Task<IActionResult> Handle(string workflowInstanceId, DispatchWorkflowInstanceRequestModel request, CancellationToken cancellationToken = default)
        {
            await _workflowLaunchpad.DispatchPendingWorkflowAsync(workflowInstanceId, request.ActivityId, request.Input, cancellationToken);

            return Json(new DispatchWorkflowInstanceResponseModel(), _serializerSettingsProvider.GetSettings());
        }
    }
}