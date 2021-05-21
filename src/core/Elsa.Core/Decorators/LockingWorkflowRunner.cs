using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Decorators
{
    public class LockingWorkflowInstanceExecutor : IWorkflowInstanceExecutor
    {
        private readonly IWorkflowInstanceExecutor _workflowInstanceExecutor;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ElsaOptions _elsaOptions;

        public LockingWorkflowInstanceExecutor(
            IWorkflowInstanceExecutor workflowInstanceExecutor,
            IDistributedLockProvider distributedLockProvider,
            ElsaOptions elsaOptions)
        {
            _workflowInstanceExecutor = workflowInstanceExecutor;
            _distributedLockProvider = distributedLockProvider;
            _elsaOptions = elsaOptions;
        }

        public async Task<RunWorkflowResult> ExecuteAsync(string workflowInstanceId, string? activityId, object? input = default, CancellationToken cancellationToken = default)
        {
            var key = $"workflow-instance:{workflowInstanceId}";
            await using var handle = await _distributedLockProvider.AcquireLockAsync(key, _elsaOptions.DistributedLockTimeout, cancellationToken);

            if (handle == null)
                throw new LockAcquisitionException("Could not acquire a lock within the configured amount of time");

            var result = await _workflowInstanceExecutor.ExecuteAsync(workflowInstanceId, activityId, input, cancellationToken);
            await handle.DisposeAsync();
            return result;
        }

        public async Task<RunWorkflowResult> ExecuteAsync(WorkflowInstance workflowInstance, string? activityId, object? input = default, CancellationToken cancellationToken = default) => 
            await _workflowInstanceExecutor.ExecuteAsync(workflowInstance, activityId, input, cancellationToken);
    }
}