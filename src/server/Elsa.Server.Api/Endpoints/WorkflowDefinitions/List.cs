using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowDefinitions;
using Elsa.Serialization;
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
        private readonly IContentSerializer _serializer;
        private readonly IMapper _mapper;
        private readonly ITenantAccessor _tenantAccessor;

        public List(IWorkflowDefinitionStore workflowDefinitionStore, IContentSerializer serializer, IMapper mapper, ITenantAccessor tenantAccessor)
        {
            _workflowDefinitionStore = workflowDefinitionStore;
            _serializer = serializer;
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
        public async Task<ActionResult<PagedList<WorkflowDefinitionSummaryModel>>> Handle(int? page = default, int? pageSize = default, VersionOptions? version = default, CancellationToken cancellationToken = default)
        {
            var tenantId = await _tenantAccessor.GetTenantIdAsync(cancellationToken);
            version ??= VersionOptions.Latest;
            var specification = new VersionOptionsSpecification(version.Value).And(new TenantSpecification<WorkflowDefinition>(tenantId));
            var totalCount = await _workflowDefinitionStore.CountAsync(specification, cancellationToken);
            var paging = page == null || pageSize == null ? default : Paging.Page(page.Value, pageSize.Value);
            var items = await _workflowDefinitionStore.FindManyAsync(specification, paging: paging, cancellationToken: cancellationToken);
            var summaries = _mapper.Map<IList<WorkflowDefinitionSummaryModel>>(items);
            var pagedList = new PagedList<WorkflowDefinitionSummaryModel>(summaries, page, pageSize, totalCount);

            return Json(pagedList, _serializer.GetSettings());
        }
    }
}