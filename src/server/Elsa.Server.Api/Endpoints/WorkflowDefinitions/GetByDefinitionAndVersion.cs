using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowDefinitions;
using Elsa.Server.Api.Helpers;
using Elsa.Server.Api.Swagger.Examples;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-definitions/{workflowDefinitionId}/{versionOptions}")]
    [Produces("application/json")]
    public class GetByDefinitionAndVersion : Controller
    {
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
        public GetByDefinitionAndVersion(IWorkflowDefinitionStore workflowDefinitionStore) => _workflowDefinitionStore = workflowDefinitionStore;

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkflowDefinition))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WorkflowDefinitionExample))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Returns a single workflow definition.",
            Description = "Returns a single workflow definition using the specified workflow definition ID and version options.",
            OperationId = "WorkflowDefinitions.GetByDefinitionAndVersion",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<IActionResult> Handle(string workflowDefinitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
        {
            var workflowDefinition = await _workflowDefinitionStore.FindAsync(new WorkflowDefinitionIdSpecification(workflowDefinitionId, versionOptions), cancellationToken);

            if (workflowDefinition == null && versionOptions.IsLatest)
                workflowDefinition = (await _workflowDefinitionStore.FindManyAsync(new WorkflowDefinitionIdSpecification(workflowDefinitionId, VersionOptions.All),
                    new OrderBy<WorkflowDefinition>(x => x.Version, SortDirection.Descending), new Paging(0, 1), cancellationToken)).FirstOrDefault();

            return workflowDefinition == null ? NotFound() : Json(workflowDefinition, SerializationHelper.GetSettingsForWorkflowDefinition());
        }
    }
}