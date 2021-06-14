using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Services;
using Elsa.Services.Dispatch;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;
using IDistributedLockProvider = Elsa.Services.IDistributedLockProvider;

namespace Elsa.StartupTasks
{
    /// <summary>
    /// If there are workflows in the Running state while the server starts, it means the workflow instance never finished execution, e.g. because the workflow host terminated.
    /// This startup task resumes these workflows.
    /// </summary>
    public class ContinueRunningWorkflows : IStartupTask
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowInstanceDispatcher _workflowInstanceDispatcher;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ElsaOptions _elsaOptions;
        private readonly ILogger _logger;

        public ContinueRunningWorkflows(
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowInstanceDispatcher workflowInstanceDispatcher,
            IDistributedLockProvider distributedLockProvider,
            ElsaOptions elsaOptions,
            ILogger<ContinueRunningWorkflows> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _workflowInstanceDispatcher = workflowInstanceDispatcher;
            _distributedLockProvider = distributedLockProvider;
            _elsaOptions = elsaOptions;
            _logger = logger;
        }

        public int Order => 1000;

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await using var handle = await _distributedLockProvider.AcquireLockAsync(GetType().Name, _elsaOptions.DistributedLockTimeout, cancellationToken);

            if (handle == null)
                return;

            var instances = await _workflowInstanceStore.FindManyAsync(new WorkflowStatusSpecification(WorkflowStatus.Running), cancellationToken: cancellationToken).ToList();

            if (instances.Any())
                _logger.LogInformation("Found {WorkflowInstanceCount} workflows with status 'Running'. Resuming each one of them", instances.Count);
            else
                _logger.LogInformation("Found no workflows with status 'Running'. Nothing to resume");

            foreach (var instance in instances)
            {
                await using var correlationLockHandle = await _distributedLockProvider.AcquireLockAsync(instance.CorrelationId, _elsaOptions.DistributedLockTimeout, cancellationToken);
                
                if(handle == null)
                {
                    _logger.LogWarning("Failed to acquire lock on correlation {CorrelationId} for workflow instance {WorkflowInstanceId}", instance.CorrelationId, instance.Id);
                    continue;
                }

                _logger.LogInformation("Resuming {WorkflowInstanceId}", instance.Id);
                var scheduledActivities = instance.ScheduledActivities;

                if (instance.CurrentActivity == null && !scheduledActivities.Any())
                {
                    if (instance.BlockingActivities.Any())
                    {
                        _logger.LogWarning(
                            "Workflow '{WorkflowInstanceId}' was in the Running state, but has no scheduled activities not has a currently executing one. However, it does have blocking activities, so switching to Suspended status",
                            instance.Id);
                        
                        instance.WorkflowStatus = WorkflowStatus.Suspended;
                        await _workflowInstanceStore.SaveAsync(instance, cancellationToken);
                        continue;
                    }

                    _logger.LogWarning("Workflow '{WorkflowInstanceId}' was in the Running state, but has no scheduled activities nor has a currently executing one", instance.Id);
                    continue;
                }

                var scheduledActivity = instance.CurrentActivity ?? instance.ScheduledActivities.Peek();

                await _workflowInstanceDispatcher.DispatchAsync(new ExecuteWorkflowInstanceRequest(instance.Id, scheduledActivity.ActivityId), cancellationToken);
            }
        }
    }
}