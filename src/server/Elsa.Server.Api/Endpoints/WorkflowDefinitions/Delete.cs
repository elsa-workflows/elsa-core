using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-definitions/{id}")]
    [Produces("application/json")]
    public class Delete : ControllerBase
    {
        private readonly IWorkflowPublisher _workflowPublisher;
        public Delete(IWorkflowPublisher workflowPublisher) => _workflowPublisher = workflowPublisher;

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [SwaggerOperation(
            Summary = "Deletes a workflow definition and all of its versions and workflow instances.",
            Description = "Deletes a workflow definition and all of its versions and workflow instances.",
            OperationId = "WorkflowDefinitions.Delete",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<IActionResult> Handle(string id, CancellationToken cancellationToken)
        {
            await _workflowPublisher.DeleteAsync(id, cancellationToken);
            return Accepted();
        }
    }
}