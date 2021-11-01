using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Persistence.Specifications.WorkflowSettings;
using Elsa.WorkflowSettings.Extensions;
using Elsa.WorkflowSettings.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.WorkflowSettings.Api.Endpoints
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-settings/bulk")]
    [Produces("application/json")]
    public class BulkDelete : Controller
    {
        private readonly IWorkflowSettingsStore _workflowSettingsStore;

        public BulkDelete(IWorkflowSettingsStore workflowSettingsStore)
        {
            _workflowSettingsStore = workflowSettingsStore;
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Deletes a workflow settings.",
            Description = "Deletes a workflow settings.",
            OperationId = "WorkflowSettings.BulkDelete",
            Tags = new[] { "WorkflowSettings" })
        ]
        public async Task<IActionResult> Handle(ICollection<string> workflowSettingsIds, CancellationToken cancellationToken = default)
        {
            var idsToDelete = new List<string>();

            foreach (var id in workflowSettingsIds) 
            { 
                var workflowSettings = await _workflowSettingsStore.FindByIdAsync(id, cancellationToken);

                if (workflowSettings != null)
                {
                    idsToDelete.Add(id);
                } 
            }

            await _workflowSettingsStore.DeleteManyAsync(new WorkflowSettingsIdsSpecification(idsToDelete), cancellationToken);
            return NoContent();
        }
    }
}