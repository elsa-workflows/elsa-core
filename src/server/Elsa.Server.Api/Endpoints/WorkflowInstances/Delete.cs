using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Repositories;
using Elsa.Server.Api.Models;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Open.Linq.AsyncExtensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowInstances
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-instances/{id}")]
    [Produces("application/json")]
    public class Delete : Controller
    {
        private readonly IWorkflowInstanceRepository _workflowInstanceRepository;

        public Delete(IWorkflowInstanceRepository workflowInstanceRepository)
        {
            _workflowInstanceRepository = workflowInstanceRepository;
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Deletes a workflow instance.",
            Description = "Deletes a workflow instance.",
            OperationId = "WorkflowInstances.Delete",
            Tags = new[] { "WorkflowInstances" })
        ]
        public async Task<IActionResult> Handle(string id, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowInstanceRepository.GetByIdAsync(id, cancellationToken);

            if (workflowInstance == null)
                return NotFound();
            
            await _workflowInstanceRepository.DeleteAsync(workflowInstance, cancellationToken);
            return NoContent();
        }
    }
}