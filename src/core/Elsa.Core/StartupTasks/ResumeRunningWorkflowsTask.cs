using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Runtime;
using Elsa.Services;

namespace Elsa.StartupTasks
{
    /// <summary>
    /// If there are workflows in the Running state while the server starts, it means the workflow instance never finished execution, e.g. because the workflow host terminated.
    /// This startup task resumes such workflows.
    /// </summary>
    public class ResumeRunningWorkflowsTask : IStartupTask
    {
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly IWorkflowScheduler workflowScheduler;
        private readonly IDistributedLockProvider distributedLockProvider;

        public ResumeRunningWorkflowsTask(
            IWorkflowInstanceStore workflowInstanceStore, 
            IWorkflowScheduler workflowScheduler,
            IDistributedLockProvider distributedLockProvider)
        {
            this.workflowInstanceStore = workflowInstanceStore;
            this.workflowScheduler = workflowScheduler;
            this.distributedLockProvider = distributedLockProvider;
        }
        
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (!await distributedLockProvider.AcquireLockAsync(GetType().Name, cancellationToken))
                return;
            
            var instances = await workflowInstanceStore.ListByStatusAsync(WorkflowStatus.Running, cancellationToken);

            foreach (var instance in instances)
            {
                await workflowScheduler.ScheduleNewWorkflowAsync(instance.Id, cancellationToken: cancellationToken);
            }
        }
    }
}