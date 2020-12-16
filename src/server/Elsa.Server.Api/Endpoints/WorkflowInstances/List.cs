using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
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
        private readonly IWorkflowInstanceStore _workflowInstanceStore;

        public List(IWorkflowInstanceStore workflowInstanceStore)
        {
            _workflowInstanceStore = workflowInstanceStore;
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
            var specification = Specification<WorkflowInstance>.All;
            var totalCount = await _workflowInstanceStore.CountAsync(specification, cancellationToken: cancellationToken);
            var paging = Paging.Page(page, pageSize);
            var workflowInstances = await _workflowInstanceStore.FindManyAsync(specification, paging: paging, cancellationToken: cancellationToken).ToList();
            return new PagedList<WorkflowInstance>(workflowInstances, page, pageSize, totalCount);
        }
    }
}