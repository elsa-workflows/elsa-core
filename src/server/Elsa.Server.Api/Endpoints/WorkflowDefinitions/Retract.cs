using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Server.Api.Helpers;
using Elsa.Server.Api.Swagger.Examples;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-definitions/{workflowDefinitionId}/retract")]
    [Produces("application/json")]
    public class Retract : ControllerBase
    {
        private readonly IWorkflowPublisher _workflowPublisher;
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;

        public Retract(IWorkflowPublisher workflowPublisher, IWorkflowDefinitionStore workflowDefinitionStore)
        {
            _workflowPublisher = workflowPublisher;
            _workflowDefinitionStore = workflowDefinitionStore;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(WorkflowDefinition))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerResponseExample(StatusCodes.Status202Accepted, typeof(WorkflowDefinitionExample))]
        [SwaggerOperation(
            Summary = "Retracts a published workflow definition.",
            Description = "Retracts a published workflow definition.",
            OperationId = "WorkflowDefinitions.Unpublish",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<ActionResult<WorkflowDefinition>> Handle(string workflowDefinitionId, ApiVersion apiVersion, CancellationToken cancellationToken)
        {
            var workflowDefinition = await _workflowDefinitionStore.FindByDefinitionIdAsync(workflowDefinitionId, VersionOptions.Published, cancellationToken);

            if (workflowDefinition == null)
                return NotFound();

            workflowDefinition = await _workflowPublisher.RetractAsync(workflowDefinition, cancellationToken);
            
            return AcceptedAtAction("Handle", "GetByVersionId", new { versionId = workflowDefinition.Id, apiVersion = apiVersion.ToString() }, workflowDefinition)
                .ConfigureForWorkflowDefinition();
        }
    }
}