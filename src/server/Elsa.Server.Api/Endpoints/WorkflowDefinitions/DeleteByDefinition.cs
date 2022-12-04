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
    [Route("v{apiVersion:apiVersion}/workflow-definitions/{definitionId}")]
    [Produces("application/json")]
    public class DeleteByDefinition : ControllerBase
    {
        private readonly IWorkflowPublisher _workflowPublisher;
        public DeleteByDefinition(IWorkflowPublisher workflowPublisher) => _workflowPublisher = workflowPublisher;

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [SwaggerOperation(
            Summary = "Deletes a workflow definition and all of its versions and workflow instances.",
            Description = "Deletes a workflow definition and all of its versions and workflow instances.",
            OperationId = "WorkflowDefinitions.DeleteByDefinition",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<IActionResult> Handle(string definitionId, CancellationToken cancellationToken = default)
        {
            await _workflowPublisher.DeleteAsync(definitionId, VersionOptions.All, cancellationToken);
            return Accepted();
        }
    }
}