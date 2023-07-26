using Elsa.Models;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Server.Api.Helpers;
using System.IO.Compression;
using Elsa.Persistence;
using MediatR;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-definitions/restore")]
    [Produces("application/json")]
    public class Restore : Controller
    {
        private readonly IWorkflowPublisher _workflowPublisher;
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
        private readonly IContentSerializer _contentSerializer;
        private readonly ITenantAccessor _tenantAccessor;
        public Restore(IWorkflowPublisher workflowPublisher, IWorkflowDefinitionStore workflowDefinitionStore, IContentSerializer contentSerializer, ITenantAccessor tenantAccessor)
        {
            _workflowPublisher = workflowPublisher;
            _workflowDefinitionStore = workflowDefinitionStore;
            _contentSerializer = contentSerializer;
            _tenantAccessor = tenantAccessor;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(
            Summary = "Restores workflow definitions from a previous backup.",
            Description = "Imports a archive containing workflow definition JSON files.",
            OperationId = "WorkflowDefinitions.Restore",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<IActionResult> Handle([FromForm] IFormFile? file, CancellationToken cancellationToken)
        {
            if (file == null)
                return BadRequest();

            using var zipArchive = new ZipArchive(file.OpenReadStream(), ZipArchiveMode.Read, true);
            foreach (var item in zipArchive.Entries)
            {
                if (item.Name.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase) == false) 
                    continue;

                await using var jsonFile = item.Open();
                var json = await jsonFile.ReadStringToEndAsync(cancellationToken);
                var postedModel = _contentSerializer.Deserialize<WorkflowDefinition>(json);

                var workflowDefinitionId = postedModel.DefinitionId;
                var workflowDefinition = !string.IsNullOrWhiteSpace(workflowDefinitionId) ? await _workflowPublisher.GetDraftAsync(workflowDefinitionId, cancellationToken) : default;
                var isNew = workflowDefinition == null;

                if (workflowDefinition == null)
                {
                    workflowDefinition = _workflowPublisher.New();

                    if (!string.IsNullOrWhiteSpace(workflowDefinitionId))
                        workflowDefinition.DefinitionId = workflowDefinitionId;
                }

                workflowDefinition.Activities = postedModel.Activities;
                workflowDefinition.Channel = postedModel.Channel;
                workflowDefinition.Connections = postedModel.Connections;
                workflowDefinition.Description = postedModel.Description;
                workflowDefinition.Name = postedModel.Name;
                workflowDefinition.Tag = postedModel.Tag;
                workflowDefinition.Variables = postedModel.Variables;
                workflowDefinition.ContextOptions = postedModel.ContextOptions;
                workflowDefinition.CustomAttributes = postedModel.CustomAttributes;
                workflowDefinition.DisplayName = postedModel.DisplayName;
                workflowDefinition.IsSingleton = postedModel.IsSingleton;
                workflowDefinition.DeleteCompletedInstances = postedModel.DeleteCompletedInstances;
                workflowDefinition.PersistenceBehavior = postedModel.PersistenceBehavior;
                workflowDefinition.TenantId = await _tenantAccessor.GetTenantIdAsync(cancellationToken);

                await _workflowPublisher.SaveDraftAsync(workflowDefinition, cancellationToken);
            }

            return Ok();
        }
    }
}