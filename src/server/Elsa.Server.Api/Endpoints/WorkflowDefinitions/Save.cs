using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Server.Api.Helpers;
using Elsa.Server.Api.Swagger.Examples;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-definitions")]
    [Produces("application/json")]
    public partial class Save : Controller
    {
        private readonly IWorkflowPublisher _workflowPublisher;
        private readonly ITenantAccessor _tenantAccessor;
        private readonly IContentSerializer _contentSerializer;

        public Save(IWorkflowPublisher workflowPublisher, ITenantAccessor tenantAccessor, IContentSerializer contentSerializer)
        {
            _workflowPublisher = workflowPublisher;
            _tenantAccessor = tenantAccessor;
            _contentSerializer = contentSerializer;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(WorkflowDefinition))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WorkflowDefinitionExample))]
        [SwaggerOperation(
            Summary = "Creates a new workflow definition or updates an existing one.",
            Description =
                "Creates a new workflow definition or updates an existing one. If the workflow already exists, a new draft is created and updated with the specified values. Use the Publish field to automatically publish the workflow.",
            OperationId = "WorkflowDefinitions.Post",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<ActionResult<WorkflowDefinition>> Handle([FromBody] SaveWorkflowDefinitionRequest request, [FromRoute] ApiVersion apiVersion, CancellationToken cancellationToken)
        {
            var workflowDefinitionId = request.WorkflowDefinitionId;
            var workflowDefinition = !string.IsNullOrWhiteSpace(workflowDefinitionId) ? await _workflowPublisher.GetDraftAsync(workflowDefinitionId, cancellationToken) : default;
            var isNew = workflowDefinition == null;

            if (workflowDefinition == null)
            {
                workflowDefinition = _workflowPublisher.New();

                if (!string.IsNullOrWhiteSpace(workflowDefinitionId))
                    workflowDefinition.DefinitionId = workflowDefinitionId;
            }

            if (!TryParseVariables(request.Variables, out var variables))
                return BadRequest("Cannot parse variables");
            
            if (!TryParseVariables(request.CustomAttributes, out var customAttributes))
                return BadRequest("Cannot parse customAttributes");

            workflowDefinition.Activities = request.Activities;
            workflowDefinition.Connections = FilterInvalidConnections(request).ToList();
            workflowDefinition.Description = request.Description?.Trim();
            workflowDefinition.Name = request.Name?.Trim();
            workflowDefinition.Variables = variables;
			workflowDefinition.CustomAttributes = customAttributes;
            workflowDefinition.IsSingleton = request.IsSingleton;
            workflowDefinition.PersistenceBehavior = request.PersistenceBehavior;
            workflowDefinition.DeleteCompletedInstances = request.DeleteCompletedInstances;
            workflowDefinition.ContextOptions = request.ContextOptions;
            workflowDefinition.DisplayName = request.DisplayName?.Trim();
            workflowDefinition.Tag = request.Tag?.Trim();
            workflowDefinition.Channel = request.Channel?.Trim();

            workflowDefinition.TenantId = await _tenantAccessor.GetTenantIdAsync(cancellationToken);

            if (request.Publish)
                workflowDefinition = await _workflowPublisher.PublishAsync(workflowDefinition, cancellationToken);
            else
                workflowDefinition = await _workflowPublisher.SaveDraftAsync(workflowDefinition, cancellationToken);

            if (!isNew)
                return Json(workflowDefinition, SerializationHelper.GetSettingsForWorkflowDefinition());

            return CreatedAtAction("Handle", "GetByVersionId", new { versionId = workflowDefinition.Id, apiVersion = apiVersion.ToString() }, workflowDefinition)
                .ConfigureForWorkflowDefinition();
        }

        private bool TryParseVariables(string? json, out Variables variables)
        {
            variables = new Variables();
            
            if (string.IsNullOrWhiteSpace(json))
                return true;

            try
            {
                var dictionary = _contentSerializer.Deserialize<Dictionary<string, object?>>(json);
                variables = new Variables(dictionary);

                return true;
            }
            catch (JsonReaderException e)
            {
                return false;
            }
        }

        private IEnumerable<ConnectionDefinition> FilterInvalidConnections(SaveWorkflowDefinitionRequest request)
        {
            var validConnections =
                from connection in request.Connections
                let sourceActivity = request.Activities.FirstOrDefault(x => x.ActivityId == connection.SourceActivityId)
                let targetActivity = request.Activities.FirstOrDefault(x => x.ActivityId == connection.TargetActivityId)
                where sourceActivity != null && targetActivity != null && connection.Outcome is not null and not ""
                select connection;

            return validConnections;
        }
    }
}