using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
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
    [Route("v{apiVersion:apiVersion}/workflow-definitions/{workflowDefinitionId}/publish")]
    [Produces("application/json")]
    public class Publish : ControllerBase
    {
        private readonly IWorkflowPublisher _workflowPublisher;
        public Publish(IWorkflowPublisher workflowPublisher) => _workflowPublisher = workflowPublisher;

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(WorkflowDefinition))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerResponseExample(StatusCodes.Status202Accepted, typeof(WorkflowDefinitionExample))]
        [SwaggerOperation(
            Summary = "Publishes a workflow definition.",
            Description = "Publishes a workflow definition.",
            OperationId = "WorkflowDefinitions.Publish",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<ActionResult<WorkflowDefinition>> Handle(string workflowDefinitionId, ApiVersion apiVersion, CancellationToken cancellationToken)
        {
            // Create a draft.
            var draft = await _workflowPublisher.GetDraftAsync(workflowDefinitionId, cancellationToken);

            if (draft == null)
                return NotFound();

            // Publish the draft.
            var publishedWorkflowDefinition = await _workflowPublisher.PublishAsync(draft, cancellationToken);

            return AcceptedAtAction("Handle", "GetByVersionId", new { versionId = publishedWorkflowDefinition.Id, apiVersion = apiVersion.ToString() }, publishedWorkflowDefinition)
                .ConfigureForWorkflowDefinition();
        }
    }
}