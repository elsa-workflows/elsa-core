using System.Threading;
using System.Threading.Tasks;
using Elsa.Dispatch;
using Elsa.Models;
using Elsa.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Server.Hangfire.Jobs
{
    public class WorkflowDefinitionJob
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IStartsWorkflow _startsWorkflow;
        private readonly ILogger<WorkflowDefinitionJob> _logger;

        public WorkflowDefinitionJob(IWorkflowRegistry workflowRegistry, IStartsWorkflow startsWorkflow, ILogger<WorkflowDefinitionJob> logger)
        {
            _workflowRegistry = workflowRegistry;
            _startsWorkflow = startsWorkflow;
            _logger = logger;
        }
        
        public async Task ExecuteAsync(ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = await _workflowRegistry.GetWorkflowAsync(request.WorkflowDefinitionId, request.TenantId, VersionOptions.Published, cancellationToken);

            if (workflowBlueprint == null)
            {
                _logger.LogWarning("No published workflow definition {WorkflowDefinitionId} found", request.WorkflowDefinitionId);
                return;
            }

            await _startsWorkflow.StartWorkflowAsync(workflowBlueprint, request.ActivityId, request.Input, request.CorrelationId, request.ContextId, cancellationToken);
        }
    }
}