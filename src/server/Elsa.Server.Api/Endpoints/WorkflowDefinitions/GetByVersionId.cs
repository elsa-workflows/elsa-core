using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
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
    [Route("v{apiVersion:apiVersion}/workflow-definitions/{versionId}")]
    [Produces("application/json")]
    public class GetByVersionId : Controller
    {
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;

        public GetByVersionId(IWorkflowDefinitionStore workflowDefinitionStore) => _workflowDefinitionStore = workflowDefinitionStore;

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkflowDefinition))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WorkflowDefinitionExample))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Returns a single workflow definition.",
            Description = "Returns a single workflow definition using the specified workflow definition version ID.",
            OperationId = "WorkflowDefinitions.GetByVersionId",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<IActionResult> Handle(string versionId, CancellationToken cancellationToken = default)
        {
            var workflowDefinition = await _workflowDefinitionStore.FindAsync(new WorkflowDefinitionVersionIdSpecification(versionId), cancellationToken);
            return workflowDefinition == null ? NotFound() : Json(workflowDefinition, SerializationHelper.GetSettingsForWorkflowDefinition());
        }
    }
}