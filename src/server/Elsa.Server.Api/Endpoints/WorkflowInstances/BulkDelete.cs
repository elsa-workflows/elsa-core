using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Server.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowInstances
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-instances/bulk")]
    [Produces("application/json")]
    public class BulkDelete : Controller
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IEndpointContentSerializerSettingsProvider _serializerSettingsProvider;

        public BulkDelete(IWorkflowInstanceStore workflowInstanceStore, IEndpointContentSerializerSettingsProvider serializerSettingsProvider)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _serializerSettingsProvider = serializerSettingsProvider;
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [SwaggerOperation(
            Summary = "Deletes a set of workflow instances.",
            Description = "Deletes a set of workflow instances.",
            OperationId = "WorkflowInstances.BulkDelete",
            Tags = new[] { "WorkflowInstances" })
        ]
        public async Task<IActionResult> Handle(BulkDeleteWorkflowsRequest request, CancellationToken cancellationToken = default)
        {
            var count = await _workflowInstanceStore.DeleteManyAsync(new WorkflowInstanceIdsSpecification(request.WorkflowInstanceIds), cancellationToken);

            return Json(new
            {
                DeletedWorkflowCount = count
            }, _serializerSettingsProvider.GetSettings());
        }
    }
}