using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Server.Api.Models;
using Elsa.Server.Api.Swagger;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Endpoints.WorkflowRegistry
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-registry")]
    [Produces("application/json")]
    public class List : Controller
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IContentSerializer _serializer;

        public List(IWorkflowRegistry workflowRegistry, IContentSerializer serializer)
        {
            _workflowRegistry = workflowRegistry;
            _serializer = serializer;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedList<WorkflowDefinition>))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WorkflowDefinitionPagedListExample))]
        [SwaggerOperation(
            Summary = "Returns a list of workflow blueprints.",
            Description = "Returns paginated a list of workflow blueprints. When no version options are specified, the latest version is returned.",
            OperationId = "WorkflowBlueprints.List",
            Tags = new[] { "WorkflowBlueprints" })
        ]
        public async Task<ActionResult<PagedList<WorkflowDefinition>>> Handle(int? page = default, int? pageSize = default, VersionOptions? version = default, CancellationToken cancellationToken = default)
        {
            version ??= VersionOptions.Latest;
            var workflowBlueprints = await _workflowRegistry.GetWorkflowsAsync(cancellationToken).Where(x => x.WithVersion(version.Value)).ToListAsync(cancellationToken);
            var totalCount = workflowBlueprints.Count;
            var skip = page * pageSize;
            var items = workflowBlueprints.AsEnumerable();

            if (skip != null) 
                items = items.Skip(skip.Value);

            if (pageSize != null)
                items = items.Take(pageSize.Value);

            var pagedList = new PagedList<IWorkflowBlueprint>(items.ToList(), page, pageSize, totalCount);

            return Json(pagedList, _serializer.GetSettings());
        }
    }
}