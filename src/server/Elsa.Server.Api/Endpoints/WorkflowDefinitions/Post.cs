using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/workflow-definitions")]
    public class Post : ControllerBase
    {
        private readonly IWorkflowDefinitionManager _workflowDefinitionManager;

        public Post(IWorkflowDefinitionManager workflowDefinitionManager)
        {
            _workflowDefinitionManager = workflowDefinitionManager;
        }

        [HttpPost]
        public async Task<IActionResult> Handle(CreateWorkflowDefinitionRequest request, ApiVersion apiVersion, CancellationToken cancellationToken)
        {
            var workflowDefinition = new WorkflowDefinition
            {
                WorkflowDefinitionId = request.WorkflowDefinitionId.Trim(),
                Activities = request.Activities,
                Connections = request.Connections,
                Description = request.Description?.Trim(),
                Name = request.Name?.Trim(),
                Variables = request.Variables,
                IsEnabled = request.Enabled,
                IsLatest = true,
                IsPublished = request.Publish,
                Version = 1,
                IsSingleton = request.IsSingleton,
                PersistenceBehavior = request.PersistenceBehavior,
                DeleteCompletedInstances = request.DeleteCompletedInstances
            };

            await _workflowDefinitionManager.SaveAsync(workflowDefinition, cancellationToken);
            return CreatedAtAction("Handle", "Get", new { version = apiVersion.ToString() }, workflowDefinition);
        }
    }
}