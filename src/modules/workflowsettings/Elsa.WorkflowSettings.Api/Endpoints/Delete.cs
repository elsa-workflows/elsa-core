using System.Threading;
using System.Threading.Tasks;
using Elsa.WorkflowSettings.Extensions;
using Elsa.WorkflowSettings.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.WorkflowSettings.Api.Endpoints
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-settings/{id}")]
    [Produces("application/json")]
    public class Delete : Controller
    {
        private readonly IWorkflowSettingsStore _workflowSettingsStore;

        public Delete(IWorkflowSettingsStore workflowSettingsStore)
        {
            _workflowSettingsStore = workflowSettingsStore;
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Deletes a workflow setting.",
            Description = "Deletes a workflow setting.",
            OperationId = "WorkflowSettings.Delete",
            Tags = new[] { "WorkflowSettings" })
        ]
        public async Task<IActionResult> Handle(string id, CancellationToken cancellationToken = default)
        {
            var workflowSettings = await _workflowSettingsStore.FindByIdAsync(id, cancellationToken);

            if (workflowSettings == null)
                return NotFound();

            await _workflowSettingsStore.DeleteAsync(workflowSettings, cancellationToken);
            return NoContent();
        }
    }
}