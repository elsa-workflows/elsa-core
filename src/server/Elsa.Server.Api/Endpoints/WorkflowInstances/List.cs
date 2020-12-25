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
        public async Task<ActionResult<PagedList<WorkflowInstance>>> Handle(
            [FromQuery(Name = "workflow")] string? workflowDefinitionId = default,
            [FromQuery(Name = "status")] WorkflowStatus? workflowStatus = default,
            [FromQuery] OrderBy? orderBy = default,
            [FromQuery] string? searchTerm = default,
            int page = 0,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            var specification = Specification<WorkflowInstance>.All;

            if (!string.IsNullOrWhiteSpace(workflowDefinitionId))
                specification = specification.WithWorkflowDefinition(workflowDefinitionId);

            if (workflowStatus != null)
                specification = specification.WithStatus(workflowStatus.Value);
            
            if (!string.IsNullOrWhiteSpace(searchTerm))
                specification = specification.WithWorkflowName(searchTerm);

            var orderBySpecification = default(OrderBy<WorkflowInstance>);
            
            if (orderBy != null)
            {
                orderBySpecification = orderBy switch
                {
                    OrderBy.Started => OrderBySpecification.OrderByDescending<WorkflowInstance>(x => x.CreatedAt),
                    OrderBy.Finished => OrderBySpecification.OrderByDescending<WorkflowInstance>(x => x.FinishedAt!),
                    _ => OrderBySpecification.OrderByDescending<WorkflowInstance>(x => x.FinishedAt!)
                };
            }
            
            var totalCount = await _workflowInstanceStore.CountAsync(specification, cancellationToken: cancellationToken);
            var paging = Paging.Page(page, pageSize);
            var workflowInstances = await _workflowInstanceStore.FindManyAsync(specification, orderBySpecification, paging, cancellationToken).ToList();
            return new PagedList<WorkflowInstance>(workflowInstances, page, pageSize, totalCount);
        }
    }
}