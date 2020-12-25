using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
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

        public Get(IWorkflowRegistry workflowRegistry, IWorkflowBlueprintMapper workflowBlueprintMapper)
        {
            _workflowRegistry = workflowRegistry;
            _workflowBlueprintMapper = workflowBlueprintMapper;
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
            var workflowBlueprint = await _workflowRegistry.GetWorkflowAsync(id, null, versionOptions.Value, cancellationToken);

            if (workflowBlueprint == null)
                return NotFound();

            return await _workflowBlueprintMapper.MapAsync(workflowBlueprint, cancellationToken);
        }
    }
}