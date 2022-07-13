using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Server.Api.Services;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowInstances
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-instances/bulk/cancel")]
    [Produces("application/json")]
    public class BulkCancel : Controller
    {
        private readonly IWorkflowInstanceCanceller _workflowInstanceCanceller;
        private readonly IEndpointContentSerializerSettingsProvider _serializerSettingsProvider;

        public BulkCancel(IWorkflowInstanceCanceller workflowInstanceCanceller, IEndpointContentSerializerSettingsProvider serializerSettingsProvider)
        {
            _workflowInstanceCanceller = workflowInstanceCanceller;
            _serializerSettingsProvider = serializerSettingsProvider;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [SwaggerOperation(
            Summary = "Cancels a set of workflow instances.",
            Description = "Cancels a set of workflow instances.",
            OperationId = "WorkflowInstances.BulkCancel",
            Tags = new[] { "WorkflowInstances" })
        ]
        public async Task<IActionResult> Handle(BulkCancelWorkflowsRequest request, CancellationToken cancellationToken = default)
        {
            var tasks = request.WorkflowInstanceIds.Select(x => _workflowInstanceCanceller.CancelAsync(x, cancellationToken));
            var results = await Task.WhenAll(tasks);
            var count = results.Where(x => x.Status == CancelWorkflowInstanceResultStatus.Ok);

            return Json(new
            {
                CancelledWorkflowCount = count
            }, _serializerSettingsProvider.GetSettings());
        }
    }
}