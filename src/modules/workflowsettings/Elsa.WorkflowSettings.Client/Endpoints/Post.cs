using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.WorkflowSettings.Abstractions.Persistence;
using Elsa.WorkflowSettings.Extensions;
using Elsa.WorkflowSettings.Models;
using Elsa.WorkflowSettings.Swagger.Examples;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.WorkflowSettings.Client.Endpoints
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-settings")]
    [Produces("application/json")]
    public class Save : ControllerBase
    {
        private readonly IWorkflowSettingsStore _workflowSettingsStore;
        private readonly IIdGenerator _idGenerator;

        public Save(IWorkflowSettingsStore store, IIdGenerator idGenerator)
        {
            _workflowSettingsStore = store;
            _idGenerator = idGenerator;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(WorkflowSetting))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WorkflowSettingsExample))]
        [SwaggerOperation(
            Summary = "Creates a new workflow settings or updates an existing one.",
            Description = "Creates a new workflow settings or updates an existing one.",
            OperationId = "WorkflowSettings.Post",
            Tags = new[] { "WorkflowSettings" })
        ]
        public async Task<ActionResult<WorkflowSetting>> Handle([FromBody] SaveWorkflowSettingsRequest request, [FromRoute] ApiVersion apiVersion, CancellationToken cancellationToken)
        {
            var workflowSettingsId = request.Id;
            var workflowBlueprintId = request.WorkflowBlueprintId;
            var key = request.Key;

            var workflowSettings = !string.IsNullOrWhiteSpace(workflowSettingsId)
                ? await _workflowSettingsStore.FindByWorkflowBlueprintIdAndKeyAsync(workflowBlueprintId, key, cancellationToken)
                : default;

            if (workflowSettings == null)
            {
                workflowSettings = new WorkflowSetting
                {
                    Id = !string.IsNullOrWhiteSpace(workflowSettingsId) ? workflowSettingsId : _idGenerator.Generate(),
                };
            }

            workflowSettings.WorkflowBlueprintId = request.WorkflowBlueprintId.Trim();
            workflowSettings.Key = request.Key.Trim();
            workflowSettings.Value = request.Value?.Trim();

            await _workflowSettingsStore.SaveAsync(workflowSettings, cancellationToken);

            return CreatedAtAction("Handle", "Get", new { id = workflowSettings.Id, apiVersion = apiVersion.ToString() }, workflowSettings);
        }
    }
}