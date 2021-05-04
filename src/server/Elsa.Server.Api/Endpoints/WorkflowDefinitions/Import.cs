using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-definitions/{workflowDefinitionId}/import")]
    [Produces("application/json")]
    public class Import : ControllerBase
    {
        private readonly IWorkflowPublisher _workflowPublisher;
        private readonly IContentSerializer _contentSerializer;

        public Import(IWorkflowPublisher workflowPublisher, IContentSerializer contentSerializer)
        {
            _workflowPublisher = workflowPublisher;
            _contentSerializer = contentSerializer;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [SwaggerOperation(
            Summary = "Imports a JSON file into the specified workflow definition.",
            Description = "Imports a JSON file into the specified workflow definition.",
            OperationId = "WorkflowDefinitions.Import",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<IActionResult> Handle(string workflowDefinitionId, [FromForm]IFormFile? file, CancellationToken cancellationToken)
        {
            if (file == null)
                return BadRequest();
            
            var json = await file.OpenReadStream().ReadStringToEndAsync(cancellationToken);
            var workflowDefinition = await _workflowPublisher.GetDraftAsync(workflowDefinitionId, cancellationToken) ?? _workflowPublisher.New();
            var postedModel = _contentSerializer.Deserialize<WorkflowDefinition>(json);
            
            workflowDefinition.Activities = postedModel.Activities;
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

            await _workflowPublisher.SaveDraftAsync(workflowDefinition, cancellationToken);
            return Ok(workflowDefinition);
        }
    }
}