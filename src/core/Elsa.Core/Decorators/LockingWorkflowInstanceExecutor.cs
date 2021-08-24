using System;
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

        public async Task<RunWorkflowResult> ExecuteAsync(string workflowInstanceId, string? activityId, WorkflowInput? input = default, CancellationToken cancellationToken = default)
        {
            var workflowInstanceLockKey = $"workflow-instance:{workflowInstanceId}";
            var currentWorkflowInstanceLockHandle = AmbientLockContext.GetCurrentWorkflowInstanceLock(workflowInstanceId);
            var workflowInstanceLockHandle = currentWorkflowInstanceLockHandle ?? await _distributedLockProvider.AcquireLockAsync(workflowInstanceLockKey, _elsaOptions.DistributedLockTimeout, cancellationToken);

            if (workflowInstanceLockHandle == null)
                throw new LockAcquisitionException("Could not acquire a lock within the configured amount of time");

            try
            {
                AmbientLockContext.SetCurrentWorkflowInstanceLock(workflowInstanceId, workflowInstanceLockHandle);
                var workflowInstance = await _workflowInstanceStore.FindByIdAsync(workflowInstanceId, cancellationToken);

                if (workflowInstance == null)
                {
                    _logger.LogWarning("Could not run workflow instance with ID {WorkflowInstanceId} because it does not exist", workflowInstanceId);
                    return new RunWorkflowResult(workflowInstance, activityId, false);
                }

                var correlationId = workflowInstance.CorrelationId;

                if (!string.IsNullOrWhiteSpace(correlationId))
                {
                    // We need to lock on correlation ID to prevent a race condition with WorkflowLaunchpad that is used to find workflows by correlation ID to execute.
                    // The race condition is: when a workflow instance is done executing, the BookmarkIndexer will collect bookmarks.
                    // But if in the meantime an event comes in that triggers correlated workflows, the bookmarks may not have been created yet.
                    var currentCorrelationLockHandle = AmbientLockContext.CurrentCorrelationLock;
                    var correlationLockHandle = currentCorrelationLockHandle ?? await _distributedLockProvider.AcquireLockAsync(correlationId, _elsaOptions.DistributedLockTimeout, cancellationToken);

                    if (correlationLockHandle == null)
                        throw new LockAcquisitionException($"Could not acquire a lock on correlation {correlationId} within the configured amount of time");

                    try
                    {
                        AmbientLockContext.CurrentCorrelationLock = correlationLockHandle;
                        return await _workflowInstanceExecutor.ExecuteAsync(workflowInstance, activityId, input, cancellationToken);
                    }
                    finally
                    {
                        AmbientLockContext.CurrentCorrelationLock = null;
                        await correlationLockHandle.DisposeAsync();
                    }
                }

                return await _workflowInstanceExecutor.ExecuteAsync(workflowInstance, activityId, input, cancellationToken);
            }
            finally
            {
                AmbientLockContext.DeleteCurrentWorkflowInstanceLock(workflowInstanceId);
                await workflowInstanceLockHandle.DisposeAsync();
            }
        }

        public async Task<RunWorkflowResult> ExecuteAsync(WorkflowInstance workflowInstance, string? activityId, WorkflowInput? input = default, CancellationToken cancellationToken = default) =>
            await _workflowInstanceExecutor.ExecuteAsync(workflowInstance, activityId, input, cancellationToken);
    }
}