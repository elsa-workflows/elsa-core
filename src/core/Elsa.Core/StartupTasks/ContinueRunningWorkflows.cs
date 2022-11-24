using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Options;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services;
using Elsa.Services.WorkflowStorage;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;
using IDistributedLockProvider = Elsa.Services.IDistributedLockProvider;

namespace Elsa.StartupTasks
{
    /// <summary>
    /// If there are workflows in the Running or Idle state while the server starts, it means the workflow instance never started or finished execution, e.g. because the workflow host terminated.
    /// This startup task resumes these workflows.
    /// </summary>
    public class ContinueRunningWorkflows : IStartupTask
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowInstanceDispatcher _workflowInstanceDispatcher;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly IWorkflowStorageService _workflowStorageService;
        private readonly ElsaOptions _elsaOptions;
        private readonly ILogger _logger;

        public ContinueRunningWorkflows(
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowInstanceDispatcher workflowInstanceDispatcher,
            IDistributedLockProvider distributedLockProvider,
            IWorkflowStorageService workflowStorageService,
            ElsaOptions elsaOptions,
            ILogger<ContinueRunningWorkflows> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _workflowInstanceDispatcher = workflowInstanceDispatcher;
            _distributedLockProvider = distributedLockProvider;
            _workflowStorageService = workflowStorageService;
            _elsaOptions = elsaOptions;
            _logger = logger;
        }

        public int Order => 1000;

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await using var handle = await _distributedLockProvider.AcquireLockAsync(GetType().Name, _elsaOptions.DistributedLockTimeout, cancellationToken);

            if (handle == null)
                return;

            await ResumeIdleWorkflows(cancellationToken);
            await ResumeRunningWorkflowsAsync(cancellationToken);
        }

        private async Task ResumeIdleWorkflows(CancellationToken cancellationToken)
        {
            var instances = await _workflowInstanceStore.FindManyAsync(new WorkflowStatusSpecification(WorkflowStatus.Idle), cancellationToken: cancellationToken).ToList();

            if (instances.Any())
                _logger.LogInformation("Found {WorkflowInstanceCount} workflows with status 'Idle'. Resuming each one of them", instances.Count);
            else
                _logger.LogInformation("Found no workflows with status 'Id'. Nothing to resume");

            foreach (var instance in instances)
            {
                await using var correlationLockHandle = await _distributedLockProvider.AcquireLockAsync(instance.CorrelationId, _elsaOptions.DistributedLockTimeout, cancellationToken);

                if (correlationLockHandle == null)
                {
                    _logger.LogWarning("Failed to acquire lock on correlation {CorrelationId} for workflow instance {WorkflowInstanceId}", instance.CorrelationId, instance.Id);
                    continue;
                }

                _logger.LogInformation("Resuming {WorkflowInstanceId}", instance.Id);

                var input = await GetWorkflowInputAsync(instance, cancellationToken);
                await _workflowInstanceDispatcher.DispatchAsync(new ExecuteWorkflowInstanceRequest(instance.Id, Input: input), cancellationToken);
            }
        }

        private async Task ResumeRunningWorkflowsAsync(CancellationToken cancellationToken)
        {
            var instances = await _workflowInstanceStore.FindManyAsync(new WorkflowStatusSpecification(WorkflowStatus.Running), cancellationToken: cancellationToken).ToList();

            if (instances.Any())
                _logger.LogInformation("Found {WorkflowInstanceCount} workflows with status 'Running'. Resuming each one of them", instances.Count);
            else
                _logger.LogInformation("Found no workflows with status 'Running'. Nothing to resume");

            foreach (var instance in instances)
            {
                await using var correlationLockHandle = await _distributedLockProvider.AcquireLockAsync(instance.CorrelationId, _elsaOptions.DistributedLockTimeout, cancellationToken);

                if (correlationLockHandle == null)
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
                var input = await GetWorkflowInputAsync(instance, cancellationToken);
                await _workflowInstanceDispatcher.DispatchAsync(new ExecuteWorkflowInstanceRequest(instance.Id, scheduledActivity.ActivityId, input), cancellationToken);
            }
        }

        private async Task<WorkflowInput?> GetWorkflowInputAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            var inputReference = workflowInstance.Input;

            if (inputReference == null)
                return null;
            
            var inputStorageProviderName = inputReference.ProviderName;
            var input = await _workflowStorageService.LoadAsync(inputStorageProviderName, new WorkflowStorageContext(workflowInstance, workflowInstance.DefinitionId), nameof(WorkflowInstance.Input), cancellationToken);

            return new WorkflowInput(input);
        }
    }
}