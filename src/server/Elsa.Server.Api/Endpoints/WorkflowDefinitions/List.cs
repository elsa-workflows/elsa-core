using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Server.Api.Models;
using Elsa.Server.Api.Swagger;
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
        private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
        private readonly IContentSerializer _serializer;

        public List(IWorkflowDefinitionManager workflowDefinitionManager, IContentSerializer serializer)
        {
            _workflowDefinitionManager = workflowDefinitionManager;
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
            var totalCount = await _workflowDefinitionManager.CountAsync(version, cancellationToken);
            var skip = page * pageSize;
            var items = await _workflowDefinitionManager.ListAsync(skip, pageSize, version, cancellationToken);
            var pagedList = new PagedList<WorkflowDefinition>(items.ToList(), page, pageSize, totalCount);

            return Json(pagedList, _serializer.GetSettings());
        }
    }
}