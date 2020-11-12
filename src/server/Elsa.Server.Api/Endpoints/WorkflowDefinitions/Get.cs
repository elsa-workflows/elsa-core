using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Server.Api.Swagger;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/workflow-definitions/{id:int}")]
    [Produces("application/json")]
    public class Get : Controller
    {
        private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
        private readonly IContentSerializer _serializer;

        protected Get(IWorkflowDefinitionManager workflowDefinitionManager, IContentSerializer serializer)
        {
            _workflowDefinitionManager = workflowDefinitionManager;
            _serializer = serializer;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkflowDefinition))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WorkflowDefinitionExample))]
        public async Task<IActionResult> Handle(string workflowDefinitionId, VersionOptions? version = default, CancellationToken cancellationToken = default)
        {
            var workflowDefinition = await _workflowDefinitionManager.GetAsync(workflowDefinitionId, version ?? VersionOptions.Latest, cancellationToken);
            return workflowDefinition == null ? (IActionResult) NotFound() : Json(workflowDefinition, _serializer.GetSettings());
        }
    }
}