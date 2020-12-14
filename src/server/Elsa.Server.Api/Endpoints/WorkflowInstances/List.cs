using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Server.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Open.Linq.AsyncExtensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowInstances
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-instances")]
    [Produces("application/json")]
    public class List : Controller
    {
        private readonly IWorkflowInstanceStore _workflowInstanceManager;

        public List(IWorkflowInstanceStore workflowInstanceStore)
        {
            _workflowInstanceManager = workflowInstanceStore;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedList<WorkflowInstance>))]
        [SwaggerOperation(
            Summary = "Returns a list of workflow instances.",
            Description = "Returns paginated a list of workflow instances.",
            OperationId = "WorkflowInstances.List",
            Tags = new[] { "WorkflowInstances" })
        ]
        public async Task<ActionResult<PagedList<WorkflowInstance>>> Handle(int page = 0, int pageSize = 50, CancellationToken cancellationToken = default)
        {
            var totalCount = await _workflowInstanceManager.CountAsync(cancellationToken);
            var workflowInstances = await _workflowInstanceManager.ListAsync(page, pageSize, cancellationToken).ToList();
            return new PagedList<WorkflowInstance>(workflowInstances, page, pageSize, totalCount);
        }
    }
}