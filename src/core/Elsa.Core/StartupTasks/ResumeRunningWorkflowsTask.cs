using System.Threading;
using System.Threading.Tasks;
using Elsa.DistributedLock;
using Elsa.Extensions;
using Elsa.Indexes;
using Elsa.Models;
using Elsa.Runtime;
using Elsa.Services;
using YesSql;

namespace Elsa.StartupTasks
{
    /// <summary>
    /// If there are workflows in the Running state while the server starts, it means the workflow instance never finished execution, e.g. because the workflow host terminated.
    /// This startup task resumes these workflows.
    /// </summary>
    public class ResumeRunningWorkflowsTask : IStartupTask
    {
        private readonly ISession _session;
        private readonly IWorkflowScheduler _workflowScheduler;
        private readonly IDistributedLockProvider _distributedLockProvider;

        public ResumeRunningWorkflowsTask(
            ISession session,
            IWorkflowScheduler workflowScheduler,
            IDistributedLockProvider distributedLockProvider)
        {
            _session = session;
            _workflowScheduler = workflowScheduler;
            _distributedLockProvider = distributedLockProvider;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (!await _distributedLockProvider.AcquireLockAsync(GetType().Name, cancellationToken))
                return;

            var instances = await _session.QueryWorkflowInstances<WorkflowInstanceIndex>()
                .Where(x => x.WorkflowStatus == WorkflowStatus.Running).ListAsync();

            foreach (var instance in instances)
                await _workflowScheduler.ScheduleWorkflowAsync(
                    instance.WorkflowInstanceId,
                    cancellationToken: cancellationToken);
        }
    }
}