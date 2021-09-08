using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Options;
using Elsa.Services;
using Elsa.Services.Models;

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
            var currentWorkflowInstanceLockHandle = AmbientLockContext.GetCurrentWorkflowInstanceLock(workflowInstanceId);
            var workflowInstanceLockHandle = currentWorkflowInstanceLockHandle ?? await _distributedLockProvider.AcquireLockAsync(workflowInstanceLockKey, _elsaOptions.DistributedLockTimeout, cancellationToken);

            if (workflowInstanceLockHandle == null)
                throw new LockAcquisitionException("Could not acquire a lock within the configured amount of time");

            try
            {
                AmbientLockContext.SetCurrentWorkflowInstanceLock(workflowInstanceId, workflowInstanceLockHandle);

                return await _workflowInstanceCanceller.CancelAsync(workflowInstanceId, cancellationToken);
            }
            finally
            {
                AmbientLockContext.DeleteCurrentWorkflowInstanceLock(workflowInstanceId);
                await workflowInstanceLockHandle.DisposeAsync();
            }
        }
    }
}