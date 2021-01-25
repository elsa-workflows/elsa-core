using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowDefinitions;
using Elsa.Serialization;
using Elsa.Server.Api.Models;
using Elsa.Server.Api.Swagger.Examples;
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

        public List(IWorkflowDefinitionStore workflowDefinitionStore, IContentSerializer serializer)
        {
            _workflowDefinitionStore = workflowDefinitionStore;
            _serializer = serializer;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedList<WorkflowDefinition>))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WorkflowDefinitionPagedListExample))]
        [SwaggerOperation(
            Summary = "Returns a list of workflow definitions.",
            Description = "Returns paginated a list of workflow definitions. When no version options are specified, the latest versions are returned.",
            OperationId = "WorkflowDefinitions.List",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<ActionResult<PagedList<WorkflowDefinition>>> Handle(int? page = default, int? pageSize = default, VersionOptions? version = default, CancellationToken cancellationToken = default)
        {
            version ??= VersionOptions.Latest;
            var specification = new VersionOptionsSpecification(version.Value);
            var totalCount = await _workflowDefinitionStore.CountAsync(specification, cancellationToken: cancellationToken);
            var paging = page == null || pageSize == null ? default : Paging.Page(page.Value, pageSize.Value);
            var items = await _workflowDefinitionStore.FindManyAsync(specification, paging: paging, cancellationToken: cancellationToken);
            var pagedList = new PagedList<WorkflowDefinition>(items.ToList(), page, pageSize, totalCount);

            return Json(pagedList, _serializer.GetSettings());
        }
    }
}