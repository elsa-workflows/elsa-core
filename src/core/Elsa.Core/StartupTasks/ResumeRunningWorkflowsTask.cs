using System.Threading;
using System.Threading.Tasks;
using Elsa.DistributedLock;
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
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowScheduler _workflowScheduler;
        private readonly IDistributedLockProvider _distributedLockProvider;

        public ResumeRunningWorkflowsTask(
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowScheduler workflowScheduler,
            IDistributedLockProvider distributedLockProvider)
        {
            this._workflowInstanceStore = workflowInstanceStore;
            this._workflowScheduler = workflowScheduler;
            this._distributedLockProvider = distributedLockProvider;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (!await _distributedLockProvider.AcquireLockAsync(GetType().Name, cancellationToken))
                return;

            var instances = await _workflowInstanceStore.ListByStatusAsync(WorkflowStatus.Running, cancellationToken);

            foreach (var instance in instances)
            {
                await _workflowScheduler.ScheduleNewWorkflowAsync(instance.Id, cancellationToken: cancellationToken);
            }
        }
    }
}