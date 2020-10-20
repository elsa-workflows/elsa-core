using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.AspNetCore.Mvc;
using YesSql;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/workflow-definitions")]
    public class Post : ControllerBase
    {
        private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
        private readonly IWorkflowPublisher _workflowPublisher;
        private readonly ISession _session;

        public Post(IWorkflowDefinitionManager workflowDefinitionManager, IWorkflowPublisher workflowPublisher, ISession session)
        {
            _workflowDefinitionManager = workflowDefinitionManager;
            _workflowPublisher = workflowPublisher;
            _session = session;
        }

        [HttpPost]
        public async Task<IActionResult> Handle(SaveWorkflowDefinitionRequest request, ApiVersion apiVersion, CancellationToken cancellationToken)
        {
            var workflowDefinition = await _workflowPublisher.GetDraftAsync(request.WorkflowDefinitionId, cancellationToken);

            if (workflowDefinition == null)
            {
                workflowDefinition = _workflowPublisher.New();

                if (!string.IsNullOrWhiteSpace(request.WorkflowDefinitionId))
                    workflowDefinition.WorkflowDefinitionId = request.WorkflowDefinitionId.Trim();
            }

            workflowDefinition.Activities = request.Activities;
            workflowDefinition.Connections = request.Connections;
            workflowDefinition.Description = request.Description?.Trim();
            workflowDefinition.Name = request.Name?.Trim();
            workflowDefinition.Variables = request.Variables;
            workflowDefinition.IsEnabled = request.Enabled;
            workflowDefinition.IsSingleton = request.IsSingleton;
            workflowDefinition.PersistenceBehavior = request.PersistenceBehavior;
            workflowDefinition.DeleteCompletedInstances = request.DeleteCompletedInstances;

            if (request.Publish)
                await _workflowPublisher.PublishAsync(workflowDefinition, cancellationToken);
            else
                await _workflowPublisher.SaveDraftAsync(workflowDefinition, cancellationToken);

            return CreatedAtAction("Handle", "Get", new { id = workflowDefinition.WorkflowDefinitionId, version = apiVersion.ToString() }, workflowDefinition);
        }
    }
}