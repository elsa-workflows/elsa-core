using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Serialization;
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
    [Route("v{apiVersion:apiVersion}/workflow-definitions/{workflowDefinitionVersionId}")]
    [Produces("application/json")]
    public class GetByVersionId : Controller
    {
        private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
        private readonly IContentSerializer _serializer;

        public GetByVersionId(IWorkflowDefinitionManager workflowDefinitionManager, IContentSerializer serializer)
        {
            _workflowDefinitionManager = workflowDefinitionManager;
            _serializer = serializer;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkflowDefinition))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WorkflowDefinitionExample))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Returns a single workflow definition.",
            Description = "Returns a single workflow definition using the specified workflow definition version ID.",
            OperationId = "WorkflowDefinitions.Get",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<IActionResult> Handle(string workflowDefinitionVersionId, CancellationToken cancellationToken = default)
        {
            var workflowDefinition = await _workflowDefinitionManager.GetByVersionIdAsync(workflowDefinitionVersionId, cancellationToken);
            return workflowDefinition == null ? (IActionResult) NotFound() : Json(workflowDefinition, _serializer.GetSettings());
        }
    }
}