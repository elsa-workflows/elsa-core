using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Options;
using Elsa.Services;

namespace Elsa.Decorators
{
    public class LockingWorkflowInstanceDeleter : IWorkflowInstanceDeleter
    {
        private readonly IWorkflowInstanceDeleter _workflowInstanceDeleter;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ElsaOptions _elsaOptions;

        public LockingWorkflowInstanceDeleter(IWorkflowInstanceDeleter workflowInstanceDeleter, IDistributedLockProvider distributedLockProvider, ElsaOptions elsaOptions)
        {
            _workflowInstanceDeleter = workflowInstanceDeleter;
            _distributedLockProvider = distributedLockProvider;
            _elsaOptions = elsaOptions;
        }

        public async Task<DeleteWorkflowInstanceResult> DeleteAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            var workflowInstanceLockKey = $"workflow-instance:{workflowInstanceId}";
            await using var workflowInstanceLockHandle = await _distributedLockProvider.AcquireLockAsync(workflowInstanceLockKey, _elsaOptions.DistributedLockTimeout, cancellationToken);

            if (workflowInstanceLockHandle == null)
                throw new LockAcquisitionException("Could not acquire a lock within the configured amount of time");

            return await _workflowInstanceDeleter.DeleteAsync(workflowInstanceId, cancellationToken);
        }
    }
}