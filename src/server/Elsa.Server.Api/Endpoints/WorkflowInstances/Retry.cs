using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Server.Api.Helpers;
using Elsa.Server.Api.Services;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowInstances
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-instances/{id}/retry")]
    [Produces("application/json")]
    public class Retry : Controller
    {
        private readonly IWorkflowInstanceStore _store;
        private readonly IWorkflowReviver _reviver;
        private readonly IEndpointContentSerializerSettingsProvider _serializerSettingsProvider;

        public Retry(IWorkflowInstanceStore store, IWorkflowReviver reviver, IEndpointContentSerializerSettingsProvider serializerSettingsProvider)
        {
            _store = store;
            _reviver = reviver;
            _serializerSettingsProvider = serializerSettingsProvider;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(
            Summary = "Retries a faulted workflow instance.",
            Description = "Retries a workflow instance.",
            OperationId = "WorkflowInstances.Retry",
            Tags = new[] { "WorkflowInstances" })
        ]
        public async Task<IActionResult> Handle(string id, RetryWorkflowRequest? options, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _store.FindByIdAsync(id, cancellationToken);

            if (workflowInstance == null)
                return NotFound();

            if (options?.RunImmediately == false)
            {
                workflowInstance = await _reviver.ReviveAndQueueAsync(workflowInstance, cancellationToken);

                var model = new
                {
                    WorkflowInstanceId = workflowInstance.Id,
                };

                return Accepted(model);
            }

            var runWorkflowResult = await _reviver.ReviveAndRunAsync(workflowInstance, cancellationToken);
            workflowInstance = runWorkflowResult.WorkflowInstance!;

            if (workflowInstance.WorkflowStatus == WorkflowStatus.Faulted)
                return StatusCode(500, new
                {
                    WorkflowInstanceId = workflowInstance.Id,
                    Fault = workflowInstance.Faults
                }).ConfigureForEndpoint();

            return Response.HasStarted
                ? new EmptyResult()
                : Json(new
                {
                    WorkflowInstanceId = workflowInstance.Id,
                    WorkflowStatus = workflowInstance.WorkflowStatus
                }, _serializerSettingsProvider.GetSettings());
        }
    }
}