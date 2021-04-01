using System.Threading;
using System.Threading.Tasks;
using Elsa.Dispatch;
using Elsa.Models;
using Elsa.Server.Orleans.Grains.Contracts;
using Elsa.Services;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;

namespace Elsa.Server.Orleans.Grains
{
    [StatelessWorker]
    public class WorkflowDefinitionGrain : Grain, IWorkflowDefinitionGrain
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IStartsWorkflow _startsWorkflow;
        private readonly ILogger<WorkflowDefinitionGrain> _logger;

        public WorkflowDefinitionGrain(IWorkflowRegistry workflowRegistry, IStartsWorkflow startsWorkflow, ILogger<WorkflowDefinitionGrain> logger)
        {
            _workflowRegistry = workflowRegistry;
            _startsWorkflow = startsWorkflow;
            _logger = logger;
        }
        
        public async Task ExecuteWorkflowAsync(ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
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