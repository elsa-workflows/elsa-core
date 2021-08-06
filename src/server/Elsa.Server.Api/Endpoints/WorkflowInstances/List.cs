using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Server.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch;

        public List(IWorkflowInstanceStore workflowInstanceStore, IMapper mapper, ILogger<List> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _mapper = mapper;
            _logger = logger;
            _stopwatch = new Stopwatch();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedList<WorkflowInstanceSummaryModel>))]
        [SwaggerOperation(
            Summary = "Returns a list of workflow instances.",
            Description = "Returns paginated a list of workflow instances.",
            OperationId = "WorkflowInstances.List",
            Tags = new[] { "WorkflowInstances" })
        ]
        public async Task<ActionResult<PagedList<WorkflowInstanceSummaryModel>>> Handle(
            [FromQuery(Name = "workflow")] string? workflowDefinitionId = default,
            [FromQuery(Name = "status")] WorkflowStatus? workflowStatus = default,
            [FromQuery] string? correlationId = default,
            [FromQuery] OrderBy? orderBy = default,
            [FromQuery] string? searchTerm = default,
            int page = 0,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            _stopwatch.Restart();
            var specification = Specification<WorkflowInstance>.Identity;

            if (!string.IsNullOrWhiteSpace(workflowDefinitionId))
                specification = specification.WithWorkflowDefinition(workflowDefinitionId);
            
            if (!string.IsNullOrWhiteSpace(correlationId))
                specification = specification.WithCorrelationId(correlationId);

            if (workflowStatus != null)
                specification = specification.WithStatus(workflowStatus.Value);
            
            if (!string.IsNullOrWhiteSpace(searchTerm)) 
                specification = specification.WithSearchTerm(searchTerm);

            var orderBySpecification = default(OrderBy<WorkflowInstance>);
            
            if (orderBy != null)
            {
                orderBySpecification = orderBy switch
                {
                    OrderBy.Started => OrderBySpecification.OrderByDescending<WorkflowInstance>(x => x.CreatedAt),
                    OrderBy.LastExecuted => OrderBySpecification.OrderByDescending<WorkflowInstance>(x => x.LastExecutedAt!),
                    OrderBy.Finished => OrderBySpecification.OrderByDescending<WorkflowInstance>(x => x.FinishedAt!),
                    _ => OrderBySpecification.OrderByDescending<WorkflowInstance>(x => x.FinishedAt!)
                };
            }
            
            var totalCount = await _workflowInstanceStore.CountAsync(specification, cancellationToken: cancellationToken);
            var paging = Paging.Page(page, pageSize);
            var workflowInstances = await _workflowInstanceStore.FindManyAsync(specification, orderBySpecification, paging, cancellationToken).ToList();
            var items = _mapper.Map<ICollection<WorkflowInstanceSummaryModel>>(workflowInstances);
            _stopwatch.Stop();
            _logger.LogDebug("Handle took {TimeElapsed}", _stopwatch.Elapsed);
            return new PagedList<WorkflowInstanceSummaryModel>(items, page, pageSize, totalCount);
        }
    }
}