using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Server.Api.Models;
using Elsa.Server.Api.Services;
using Elsa.Server.Api.Swagger.Examples;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Endpoints.WorkflowRegistry
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-registry/{id}/{versionOptions?}")]
    [Produces("application/json")]
    public class Get : Controller
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowBlueprintMapper _workflowBlueprintMapper;
        private readonly IContentSerializer _contentSerializer;

        public Get(IWorkflowRegistry workflowRegistry, IWorkflowBlueprintMapper workflowBlueprintMapper, IContentSerializer contentSerializer)
        {
            _workflowRegistry = workflowRegistry;
            _workflowBlueprintMapper = workflowBlueprintMapper;
            _contentSerializer = contentSerializer;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedList<WorkflowBlueprintModel>))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WorkflowBlueprintModelExample))]
        [SwaggerOperation(
            Summary = "Returns a single workflow blueprint.",
            Description = "Returns a single workflow blueprint. When no version options are specified, the latest version is returned.",
            OperationId = "WorkflowBlueprints.Get",
            Tags = new[] { "WorkflowBlueprints" })
        ]
        public async Task<ActionResult<WorkflowBlueprintModel>> Handle(string id, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default)
        {
            versionOptions ??= VersionOptions.Latest;
            var workflowBlueprint = await _workflowRegistry.FindAsync(id, versionOptions.Value, default, cancellationToken);

            if (workflowBlueprint == null)
                return NotFound();

            var model = await _workflowBlueprintMapper.MapAsync(workflowBlueprint, cancellationToken);
            
            // Filter out activities from composite activities.
            model.Activities = model.Activities.Where(x => x.ParentId == null || x.ParentId == workflowBlueprint.Id).ToList();
            
            return Json(model, _contentSerializer.GetSettings());
        }
    }
}