using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Decorators
{
    public class LockingWorkflowInstanceExecutor : IWorkflowInstanceExecutor
    {
        private readonly IWorkflowInstanceExecutor _workflowInstanceExecutor;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ElsaOptions _elsaOptions;
        private readonly ILogger<LockingWorkflowInstanceExecutor> _logger;

        public LockingWorkflowInstanceExecutor(
            IWorkflowInstanceExecutor workflowInstanceExecutor,
            IDistributedLockProvider distributedLockProvider,
            ElsaOptions elsaOptions,
            ILogger<LockingWorkflowInstanceExecutor> logger)
        {
            _workflowInstanceExecutor = workflowInstanceExecutor;
            _distributedLockProvider = distributedLockProvider;
            _elsaOptions = elsaOptions;
            _logger = logger;
        }

        public async Task<RunWorkflowResult> ExecuteAsync(string workflowInstanceId, string? activityId, object? input = default, CancellationToken cancellationToken = default)
        {
            var key = $"workflow-instance:{workflowInstanceId}";
            _logger.LogDebug("Acquiring lock on {LockKey}", key);
            await using var handle = await _distributedLockProvider.AcquireLockAsync(key, _elsaOptions.DistributedLockTimeout, cancellationToken);

            if (handle == null)
                throw new LockAcquisitionException("Could not acquire a lock within the configured amount of time");

            _logger.LogDebug("Lock acquired on {LockKey}", key);
            var result = await _workflowInstanceExecutor.ExecuteAsync(workflowInstanceId, activityId, input, cancellationToken);
            await handle.DisposeAsync();
            _logger.LogDebug("Released lock on {LockKey}", key);
            return result;
        }

        public async Task<RunWorkflowResult> ExecuteAsync(WorkflowInstance workflowInstance, string? activityId, object? input = default, CancellationToken cancellationToken = default) => 
            await _workflowInstanceExecutor.ExecuteAsync(workflowInstance, activityId, input, cancellationToken);
    }
}