using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Decorators
{
    public class LockingWorkflowRunner : IWorkflowRunner
    {
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ElsaOptions _elsaOptions;

        public LockingWorkflowRunner(
            IWorkflowRunner workflowRunner,
            IDistributedLockProvider distributedLockProvider,
            ElsaOptions elsaOptions)
        {
            _workflowRunner = workflowRunner;
            _distributedLockProvider = distributedLockProvider;
            _elsaOptions = elsaOptions;
        }

        public async Task<WorkflowInstance> RunWorkflowAsync(
            IWorkflowBlueprint workflowBlueprint,
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default)
        {
            var key = $"locking-workflow-runner:{workflowInstance.Id}";

            await using var handle = await _distributedLockProvider.AcquireLockAsync(key, _elsaOptions.DistributedLockTimeout, cancellationToken);
            
            if (handle == null)
                throw new LockAcquisitionException("Could not acquire a lock within the configured amount of time");

            workflowInstance =  await _workflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance, activityId, input, cancellationToken);
            await handle.DisposeAsync();
            return workflowInstance;
        }
    }
}