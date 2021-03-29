using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Elsa.DistributedLocking;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Elsa.Triggers;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;
using Rebus.Handlers;

namespace Elsa.Dispatch.Consumers
{
    public class ExecuteCorrelatedWorkflowRequestConsumer : IHandleMessages<ExecuteCorrelatedWorkflowRequest>
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly ITriggerFinder _triggerFinder;
        private readonly ICommandSender _commandSender;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch = new();

        public ExecuteCorrelatedWorkflowRequestConsumer(
            IWorkflowInstanceStore workflowInstanceStore,
            IDistributedLockProvider distributedLockProvider,
            IBookmarkFinder bookmarkFinder,
            ITriggerFinder triggerFinder,
            ICommandSender commandSender,
            ILogger<ExecuteCorrelatedWorkflowRequestConsumer> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _distributedLockProvider = distributedLockProvider;
            _bookmarkFinder = bookmarkFinder;
            _triggerFinder = triggerFinder;
            _commandSender = commandSender;
            _logger = logger;
        }

        public async Task Handle(ExecuteCorrelatedWorkflowRequest message)
        {
            var correlationId = message.CorrelationId;
            var lockKey = $"correlated-workflow-request:correlation-{correlationId}";

            _logger.LogDebug("Acquiring lock {LockKey}", lockKey);
            _stopwatch.Restart();

            if (!await _distributedLockProvider.AcquireLockAsync(lockKey))
            {
                _logger.LogDebug("Lock {LockKey} already taken", lockKey);
                await _commandSender.SendAsync(message);
                return;
            }

            try
            {
                var correlatedWorkflowInstanceCount = await _workflowInstanceStore.CountAsync(new CorrelationIdSpecification<WorkflowInstance>(correlationId));

                if (correlatedWorkflowInstanceCount > 0)
                {
                    _logger.LogDebug("{WorkflowInstanceCount} existing workflows found with correlation ID '{CorrelationId}' will be queued for execution", correlatedWorkflowInstanceCount, correlationId);
                    var existingWorkflows = await _bookmarkFinder.FindBookmarksAsync(message.ActivityType, message.Bookmark, message.TenantId).ToList();
                    await EnqueueWorkflowsAsync(existingWorkflows, message.Input);
                }
                else
                {
                    // Trigger new workflow.
                    _logger.LogDebug("No existing workflows found with correlation ID '{CorrelationId}'. Starting new workflow", correlationId);
                    await TriggerNewWorkflowAsync(message);
                }
            }
            finally
            {
                await _distributedLockProvider.ReleaseLockAsync(lockKey);
                _stopwatch.Stop();
                _logger.LogDebug("Lock held for {ElapseTime}", _stopwatch.Elapsed);
            }
        }
        
        async Task TriggerNewWorkflowAsync(ExecuteCorrelatedWorkflowRequest message)
        {
            var filter = message.Trigger;
            var triggers = await _triggerFinder.FindTriggersAsync(message.ActivityType, filter, message.TenantId);

            foreach (var trigger in triggers)
            {
                var workflowBlueprint = trigger.WorkflowBlueprint;
                await EnqueueWorkflowDefinition(workflowBlueprint.Id, workflowBlueprint.TenantId, trigger.ActivityId, message.Input, message.CorrelationId, message.ContextId);
            }
        }

        private async Task EnqueueWorkflowsAsync(IEnumerable<BookmarkFinderResult> results, object? input)
        {
            foreach (var result in results)
                await EnqueueWorkflowInstance(result.WorkflowInstanceId, result.ActivityId, input);
        }

        public async Task EnqueueWorkflowInstance(string workflowInstanceId, string activityId, object? input) => await _commandSender.SendAsync(new ExecuteWorkflowInstanceRequest(workflowInstanceId, activityId, input));

        public async Task EnqueueWorkflowDefinition(string workflowDefinitionId, string? tenantId, string activityId, object? input, string? correlationId, string? contextId) =>
            await _commandSender.SendAsync(new ExecuteWorkflowDefinitionRequest(workflowDefinitionId, activityId, input, correlationId, contextId, tenantId));
    }
}