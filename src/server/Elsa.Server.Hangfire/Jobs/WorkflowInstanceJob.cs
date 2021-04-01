using System.Threading;
using System.Threading.Tasks;
using Elsa.Dispatch;
using Elsa.Persistence;
using Elsa.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Server.Hangfire.Jobs
{
    public class WorkflowInstanceJob
    {
        private readonly IWorkflowInstanceStore _store;
        private readonly IResumesWorkflow _workflowRunner;
        private readonly ILogger<WorkflowInstanceJob> _logger;

        public WorkflowInstanceJob(IWorkflowInstanceStore store, IResumesWorkflow workflowRunner, ILogger<WorkflowInstanceJob> logger)
        {
            _store = store;
            _workflowRunner = workflowRunner;
            _logger = logger;
        }
        
        public async Task ExecuteAsync(ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
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