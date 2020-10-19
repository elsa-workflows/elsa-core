using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Queries;
using Elsa.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/workflow-definitions/{id}")]
    public class Get : ControllerBase
    {
        private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
        public Get(IWorkflowDefinitionManager workflowDefinitionManager) => _workflowDefinitionManager = workflowDefinitionManager;

        [HttpGet]
        public async Task<IActionResult> Handle([FromRoute(Name = "id")] string workflowDefinitionId, CancellationToken cancellationToken)
        {
            var workflowDefinition = await _workflowDefinitionManager.GetAsync(workflowDefinitionId, VersionOptions.Latest, cancellationToken);
            return workflowDefinition == null ? (IActionResult)NotFound() : Ok(workflowDefinition);
        }
    }
}