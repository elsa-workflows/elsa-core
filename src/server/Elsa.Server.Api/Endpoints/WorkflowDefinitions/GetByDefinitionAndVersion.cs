using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Serialization;
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
    [Route("v{apiVersion:apiVersion}/workflow-definitions/{workflowDefinitionId}/{versionOptions}")]
    [Produces("application/json")]
    public class GetByDefinitionAndVersion : Controller
    {
        private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
        private readonly IContentSerializer _serializer;

        public GetByDefinitionAndVersion(IWorkflowDefinitionManager workflowDefinitionManager, IContentSerializer serializer)
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
            Description = "Returns a single workflow definition using the specified workflow definition ID and version options.",
            OperationId = "WorkflowDefinitions.Get",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<IActionResult> Handle(string workflowDefinitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
        {
            var workflowDefinition = await _workflowDefinitionManager.GetAsync(workflowDefinitionId, versionOptions, cancellationToken);
            return workflowDefinition == null ? (IActionResult) NotFound() : Json(workflowDefinition, _serializer.GetSettings());
        }
    }
}