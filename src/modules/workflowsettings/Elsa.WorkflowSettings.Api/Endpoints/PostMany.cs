using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.WorkflowSettings.Api.Models;
using Elsa.WorkflowSettings.Api.Swagger.Examples;
using Elsa.WorkflowSettings.Extensions;
using Elsa.WorkflowSettings.Models;
using Elsa.WorkflowSettings.Persistence;
using Elsa.WorkflowSettings.Specifications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.WorkflowSettings.Api.Endpoints
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-settings/many")]
    [Produces("application/json")]
    public class PostMany : ControllerBase
    {
        private readonly IWorkflowSettingsStore _workflowSettingsStore;
        private readonly IIdGenerator _idGenerator;

        public PostMany(IWorkflowSettingsStore store, IIdGenerator idGenerator)
        {
            _workflowSettingsStore = store;
            _idGenerator = idGenerator;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(WorkflowSetting))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WorkflowSettingExample))]
        [SwaggerOperation(
            Summary = "Creates a new workflow settings or updates an existing ones.",
            Description = "Creates a new workflow settings or updates an existing ones.",
            OperationId = "WorkflowSettings.Post",
            Tags = new[] { "WorkflowSettings" })
        ]
        public async Task<ActionResult<WorkflowSetting[]>> Handle([FromBody] ICollection<SaveWorkflowSettingRequest> settingsRequest, [FromRoute] ApiVersion apiVersion, CancellationToken cancellationToken)
        {
            var workflowSettingsList = new List<WorkflowSetting>();

            foreach (var item in settingsRequest)
            {
                var workflowSettingsId = item.Id;
                var workflowBlueprintId = item.WorkflowBlueprintId;
                var key = item.Key;

                var workflowSettings = !string.IsNullOrWhiteSpace(workflowSettingsId)
                    ? await _workflowSettingsStore.FindByWorkflowBlueprintIdAndKeyAsync(workflowBlueprintId, key, cancellationToken)
                    : default;

                if (workflowSettings == null)
                {
                    var existingSetting = await _workflowSettingsStore.FindAsync(new WorkflowSettingsBlueprintIdKeySpecification(key, workflowBlueprintId));

                    if (existingSetting != null)
                        return BadRequest($"Setting with key {key} already exist. Please choose another one");

                    workflowSettings = new WorkflowSetting
                    {
                        Id = !string.IsNullOrWhiteSpace(workflowSettingsId) ? workflowSettingsId : _idGenerator.Generate(),
                    };
                }

                workflowSettings.WorkflowBlueprintId = item.WorkflowBlueprintId.Trim();
                workflowSettings.Key = item.Key.Trim();
                workflowSettings.Value = item.Value?.Trim();
                workflowSettings.Description = item.Description.Trim();
                workflowSettings.DefaultValue = item.DefaultValue?.Trim();

                workflowSettingsList.Add(workflowSettings);
            }

            await _workflowSettingsStore.SaveManyAsync(workflowSettingsList, cancellationToken); 

            return CreatedAtAction("Handle", "Get", workflowSettingsList );
        }
    }
}