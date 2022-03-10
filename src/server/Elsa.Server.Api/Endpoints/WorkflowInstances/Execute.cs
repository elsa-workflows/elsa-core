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
    [Route("v{apiVersion:apiVersion}/workflow-instances/{workflowInstanceId}/execute")]
    [Produces("application/json")]
    public class Execute : Controller
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        private readonly IEndpointContentSerializerSettingsProvider _serializerSettingsProvider;

        public Execute(IWorkflowLaunchpad workflowLaunchpad, IEndpointContentSerializerSettingsProvider serializerSettingsProvider)
        {
            _workflowLaunchpad = workflowLaunchpad;
            _serializerSettingsProvider = serializerSettingsProvider;
        }

        [HttpPost]
        [ElsaJsonFormatter]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExecuteWorkflowInstanceResponseModel))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Executes the specified workflow instance.",
            Description = "Executes the specified workflow instance.",
            OperationId = "WorkflowInstances.Execute",
            Tags = new[] { "WorkflowInstances" })
        ]
        public async Task<IActionResult> Handle(string workflowInstanceId, ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _workflowLaunchpad.ExecutePendingWorkflowAsync(workflowInstanceId, request.ActivityId, request.Input, cancellationToken);

            if (Response.HasStarted)
                return new EmptyResult();

            return Json(new ExecuteWorkflowInstanceResponseModel(result.Executed, result.ActivityId, result.WorkflowInstance), _serializerSettingsProvider.GetSettings());
        }
    }
}