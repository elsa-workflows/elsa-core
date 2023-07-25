using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowDefinitions;
using Elsa.Server.Api.Helpers;
using Elsa.Server.Api.Models;
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
    [Route("v{apiVersion:apiVersion}/workflow-definitions")]
    [Produces("application/json")]
    public class List : Controller
    {
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
        private readonly IMapper _mapper;
        private readonly ITenantAccessor _tenantAccessor;

        public List(IWorkflowDefinitionStore workflowDefinitionStore, IMapper mapper, ITenantAccessor tenantAccessor)
        {
            _workflowDefinitionStore = workflowDefinitionStore;
            _mapper = mapper;
            _tenantAccessor = tenantAccessor;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedList<WorkflowDefinitionSummaryModel>))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WorkflowDefinitionPagedListExample))]
        [SwaggerOperation(
            Summary = "Returns a list of workflow definitions.",
            Description = "Returns paginated a list of workflow definition summaries. When no version options are specified, the latest versions are returned.",
            OperationId = "WorkflowDefinitions.List",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<ActionResult<PagedList<WorkflowDefinitionSummaryModel>>> Handle(
            [FromQuery] string? ids,
            [FromQuery] string? searchTerm = default,
            [FromQuery] WorkflowDefinitionOrderBy? orderBy = WorkflowDefinitionOrderBy.Name,
            [FromQuery] SortBy? sortBy = SortBy.Ascending,
            int? page = default,
            int? pageSize = default,
            VersionOptions? version = default,
            CancellationToken cancellationToken = default)
        {
            var tenantId = await _tenantAccessor.GetTenantIdAsync(cancellationToken);
            version ??= VersionOptions.Latest;
            var specification = GetSpecification(ids, version.Value).And(new TenantSpecification<WorkflowDefinition>(tenantId));
           
            if (!string.IsNullOrWhiteSpace(searchTerm)) 
                specification = specification.And(new WorkflowDefinitionSearchTermSpecification(searchTerm));

            var orderBySpecification = default(OrderBy<WorkflowDefinition>);

            orderBySpecification = orderBy switch
            {
                WorkflowDefinitionOrderBy.Name => OrderBySpecification.OrderBy<WorkflowDefinition>(x => x.Name!, (SortDirection)sortBy!.Value),
                WorkflowDefinitionOrderBy.Description => OrderBySpecification.OrderBy<WorkflowDefinition>(x => x.Description!, (SortDirection)sortBy!.Value),
                WorkflowDefinitionOrderBy.CreatedAt => OrderBySpecification.OrderBy<WorkflowDefinition>(x => x.CreatedAt!, (SortDirection)sortBy!.Value),
                _ => OrderBySpecification.OrderBy<WorkflowDefinition>(x => x.Name!, (SortDirection)sortBy!.Value)
            };
            
            var totalCount = await _workflowDefinitionStore.CountAsync(specification, cancellationToken);
            var paging = page == null || pageSize == null ? default : Paging.Page(page.Value, pageSize.Value);
            var items = await _workflowDefinitionStore.FindManyAsync(specification, orderBySpecification, paging, cancellationToken);
            var summaries = _mapper.Map<IList<WorkflowDefinitionSummaryModel>>(items);
            var pagedList = new PagedList<WorkflowDefinitionSummaryModel>(summaries, page, pageSize, totalCount);

            return Json(pagedList, SerializationHelper.GetSettingsForWorkflowDefinition());
        }

        internal static Specification<WorkflowDefinition> GetSpecification(string? ids, VersionOptions version)
        {
            if (string.IsNullOrWhiteSpace(ids)) 
                return new VersionOptionsSpecification(version);
            
            var splitIds = ids.Split(',', StringSplitOptions.RemoveEmptyEntries);
            return new ManyWorkflowDefinitionIdsSpecification(splitIds, version);
        }

    }
}