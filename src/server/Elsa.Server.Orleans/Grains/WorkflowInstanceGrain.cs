using System.Threading;
using System.Threading.Tasks;
using Elsa.Dispatch;
using Elsa.Persistence;
using Elsa.Server.Orleans.Grains.Contracts;
using Elsa.Services;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Elsa.Server.Orleans.Grains
{
    public class WorkflowInstanceGrain : Grain, IWorkflowInstanceGrain
    {
        private readonly IWorkflowInstanceStore _store;
        private readonly IResumesWorkflow _workflowRunner;
        private readonly ILogger<WorkflowInstanceGrain> _logger;

        public WorkflowInstanceGrain(IWorkflowInstanceStore store, IResumesWorkflow workflowRunner, ILogger<WorkflowInstanceGrain> logger)
        {
            _store = store;
            _workflowRunner = workflowRunner;
            _logger = logger;
        }
        
        public async Task ExecuteWorkflowAsync(ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _store.FindByIdAsync(request.WorkflowInstanceId, cancellationToken);
            
            if(workflowInstance == null)
            {
                _logger.LogWarning("Workflow instance {WorkflowInstanceId} not found", request.WorkflowInstanceId);
                return;
            }
            
            await _workflowRunner.ResumeWorkflowAsync(workflowInstance, request.ActivityId, request.Input, cancellationToken);
        }
    }
}