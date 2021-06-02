using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Decorators
{
    public class LockingWorkflowInstanceExecutor : IWorkflowInstanceExecutor
    {
        private readonly IWorkflowInstanceExecutor _workflowInstanceExecutor;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ElsaOptions _elsaOptions;
        private readonly ILogger<LockingWorkflowInstanceExecutor> _logger;

        public LockingWorkflowInstanceExecutor(
            IWorkflowInstanceExecutor workflowInstanceExecutor,
            IWorkflowInstanceStore workflowInstanceStore,
            IDistributedLockProvider distributedLockProvider,
            ElsaOptions elsaOptions,
            ILogger<LockingWorkflowInstanceExecutor> logger)
        {
            _workflowInstanceExecutor = workflowInstanceExecutor;
            _workflowInstanceStore = workflowInstanceStore;
            _distributedLockProvider = distributedLockProvider;
            _elsaOptions = elsaOptions;
            _logger = logger;
        }

        public async Task<RunWorkflowResult> ExecuteAsync(string workflowInstanceId, string? activityId, object? input = default, CancellationToken cancellationToken = default)
        {
            var workflowInstanceLockKey = $"workflow-instance:{workflowInstanceId}";
            _logger.LogDebug("Acquiring lock on {LockKey}", workflowInstanceLockKey);
            await using var workflowInstanceLockHandle = await _distributedLockProvider.AcquireLockAsync(workflowInstanceLockKey, _elsaOptions.DistributedLockTimeout, cancellationToken);

            if (workflowInstanceLockHandle == null)
                throw new LockAcquisitionException("Could not acquire a lock within the configured amount of time");

            _logger.LogDebug("Lock acquired on {LockKey}", workflowInstanceLockKey);
            var workflowInstance = await _workflowInstanceStore.FindByIdAsync(workflowInstanceId, cancellationToken);

            if (workflowInstance == null)
            {
                _logger.LogWarning("Could not run workflow instance with ID {WorkflowInstanceId} because it does not exist", workflowInstanceId);
                return new RunWorkflowResult(workflowInstance, activityId, false);
            }

            var correlationId = workflowInstance.CorrelationId;

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                _logger.LogDebug("Acquiring lock on correlation {CorrelationId}", correlationId);
                await using var correlationLockHandle = await _distributedLockProvider.AcquireLockAsync(correlationId, _elsaOptions.DistributedLockTimeout, cancellationToken);
                
                if(correlationLockHandle == null)
                    throw new LockAcquisitionException($"Could not acquire a lock on correlation {correlationId} within the configured amount of time");
                
                _logger.LogDebug("Lock acquired on correlation {CorrelationId}", correlationId);
                var result = await _workflowInstanceExecutor.ExecuteAsync(workflowInstance, activityId, input, cancellationToken);
                _logger.LogDebug("Released lock on correlation {CorrelationId}", correlationId);
                _logger.LogDebug("Released lock on {LockKey}", workflowInstanceLockKey);
                return result;
            }
            else
            {
                var result = await _workflowInstanceExecutor.ExecuteAsync(workflowInstance, activityId, input, cancellationToken);
                _logger.LogDebug("Released lock on {LockKey}", workflowInstanceLockKey);
                return result;
            }
        }

        public async Task<RunWorkflowResult> ExecuteAsync(WorkflowInstance workflowInstance, string? activityId, object? input = default, CancellationToken cancellationToken = default) =>
            await _workflowInstanceExecutor.ExecuteAsync(workflowInstance, activityId, input, cancellationToken);
    }
}