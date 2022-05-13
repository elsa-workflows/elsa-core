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
    [Route("v{apiVersion:apiVersion}/workflow-definitions/{definitionId}/revert/{version}")]
    [Produces("application/json")]
    public class Revert : ControllerBase
    {
        private readonly IWorkflowPublisher _workflowPublisher;
        public Revert(IWorkflowPublisher workflowPublisher) => _workflowPublisher = workflowPublisher;

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkflowDefinition))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WorkflowDefinitionExample))]
        [SwaggerOperation(
            Summary = "Reverts a workflow definition to the specified version and returns it as a new draft.",
            Description = "Reverts a workflow definition to the specified version and returns it as a new draft.",
            OperationId = "WorkflowDefinitions.Revert",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<ActionResult<WorkflowDefinition>> Handle(string definitionId, int version, ApiVersion apiVersion, CancellationToken cancellationToken)
        {
            // Create a draft based on the specified version.
            var draft = await _workflowPublisher.GetDraftAsync(definitionId, VersionOptions.SpecificVersion(version), cancellationToken);

            if (draft == null)
                return NotFound();

            // Save the draft.
            await _workflowPublisher.SaveDraftAsync(draft, cancellationToken);

            return AcceptedAtAction("Handle", "GetByVersionId", new { versionId = draft.Id, apiVersion = apiVersion.ToString() }, draft)
                .ConfigureForWorkflowDefinition();
        }
    }
}