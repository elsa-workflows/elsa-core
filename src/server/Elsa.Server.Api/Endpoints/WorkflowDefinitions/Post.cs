using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Server.Api.Swagger;
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
    public class Post : ControllerBase
    {
        private readonly IWorkflowDefinitionManager _workflowDefinitionManager;

        public Post(IWorkflowDefinitionManager workflowDefinitionManager)
        {
            _workflowDefinitionManager = workflowDefinitionManager;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(WorkflowDefinition))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WorkflowDefinitionExample))]
        [SwaggerOperation(
            Summary = "Creates a new workflow definition or updates an existing one.",
            Description = "Creates a new workflow definition or updates an existing one.",
            OperationId = "WorkflowDefinitions.Post",
            Tags = new[] {"WorkflowDefinitions"})
        ]
        public async Task<ActionResult<WorkflowDefinition>> Handle(WorkflowDefinition workflowDefinition, ApiVersion apiVersion, CancellationToken cancellationToken)
        {
            await _workflowDefinitionManager.SaveAsync(workflowDefinition, cancellationToken);
            return CreatedAtAction("Handle", "GetByVersionId", new {workflowDefinitionVersionId = workflowDefinition.WorkflowDefinitionVersionId, apiVersion = apiVersion.ToString()}, workflowDefinition);
        }
    }
}