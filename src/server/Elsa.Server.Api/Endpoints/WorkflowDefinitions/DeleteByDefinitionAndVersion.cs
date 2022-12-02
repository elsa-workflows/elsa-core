using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-definitions/{definitionId}/{versionOptions}")]
    [Produces("application/json")]
    public class DeleteByDefinitionAndVersion : ControllerBase
    {
        private readonly IWorkflowPublisher _workflowPublisher;
        public DeleteByDefinitionAndVersion(IWorkflowPublisher workflowPublisher) => _workflowPublisher = workflowPublisher;

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [SwaggerOperation(
            Summary = "Deletes a workflow definition in a specified version or all of its versions and workflow instances.",
            Description = "Deletes a workflow definition in a specified version or all of its versions and workflow instances.",
            OperationId = "WorkflowDefinitions.DeleteByDefinitionAndVersion",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<IActionResult> Handle(string definitionId, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default)
        {
            if (versionOptions == null)
                await _workflowPublisher.DeleteAsync(definitionId, VersionOptions.All, cancellationToken);
            else
                await _workflowPublisher.DeleteAsync(definitionId, versionOptions.Value, cancellationToken);

            return Accepted();
        }
    }
}