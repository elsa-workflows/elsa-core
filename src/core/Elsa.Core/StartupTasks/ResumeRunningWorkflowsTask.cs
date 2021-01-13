using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.DistributedLock;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

namespace Elsa.StartupTasks
{
    /// <summary>
    /// If there are workflows in the Running state while the server starts, it means the workflow instance never finished execution, e.g. because the workflow host terminated.
    /// This startup task resumes these workflows.
    /// </summary>
    public class ResumeRunningWorkflowsTask : IStartupTask
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowRunner _workflowScheduler;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ILogger<ResumeRunningWorkflowsTask> _logger;

        public ResumeRunningWorkflowsTask(
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowRunner workflowScheduler,
            IDistributedLockProvider distributedLockProvider,
            ILogger<ResumeRunningWorkflowsTask> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _workflowScheduler = workflowScheduler;
            _distributedLockProvider = distributedLockProvider;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var lockKey = GetType().Name;
            
            if (!await _distributedLockProvider.AcquireLockAsync(lockKey, cancellationToken))
                return;

            try
            {
                var instances = await _workflowInstanceStore.FindManyAsync(new WorkflowStatusSpecification(WorkflowStatus.Running), cancellationToken: cancellationToken).ToList();
                
                _logger.LogInformation("Found {WorkflowInstanceCount} workflows with status 'Running'. Resuming each one of them.", instances.Count);
                
                foreach (var instance in instances)
                {
                    _logger.LogInformation("Resuming {WorkflowInstanceId}", instance.Id);
                    await _workflowScheduler.RunWorkflowAsync(
                        instance,
                        cancellationToken: cancellationToken);
                }
            }
            finally
            {
                await _distributedLockProvider.ReleaseLockAsync(lockKey, cancellationToken);
            }
        }
    }
}