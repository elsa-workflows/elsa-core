using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Options;
using Elsa.Services;

namespace Elsa.Decorators
{
    public class LockingWorkflowInstanceCanceller : IWorkflowInstanceCanceller
    {
        private readonly IWorkflowInstanceCanceller _workflowInstanceCanceller;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ElsaOptions _elsaOptions;

        public LockingWorkflowInstanceCanceller(IWorkflowInstanceCanceller workflowInstanceCanceller, IDistributedLockProvider distributedLockProvider, ElsaOptions elsaOptions)
        {
            _workflowInstanceCanceller = workflowInstanceCanceller;
            _distributedLockProvider = distributedLockProvider;
            _elsaOptions = elsaOptions;
        }

        public async Task<CancelWorkflowInstanceResult> CancelAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            var workflowInstanceLockKey = $"workflow-instance:{workflowInstanceId}";
            await using var workflowInstanceLockHandle = await _distributedLockProvider.AcquireLockAsync(workflowInstanceLockKey, _elsaOptions.DistributedLockTimeout, cancellationToken);

            if (workflowInstanceLockHandle == null)
                throw new LockAcquisitionException("Could not acquire a lock within the configured amount of time");

            return await _workflowInstanceCanceller.CancelAsync(workflowInstanceId, cancellationToken);
        }
    }
}