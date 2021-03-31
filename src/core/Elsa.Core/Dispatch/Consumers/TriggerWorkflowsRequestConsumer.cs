using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Bookmarks;
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
    public class TriggerWorkflowsRequestConsumer : IHandleMessages<TriggerWorkflowsRequest>
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly ITriggerFinder _triggerFinder;
        private readonly ICommandSender _commandSender;
        private readonly ILogger _logger;

        public TriggerWorkflowsRequestConsumer(
            IWorkflowInstanceStore workflowInstanceStore,
            IBookmarkFinder bookmarkFinder,
            ITriggerFinder triggerFinder,
            ICommandSender commandSender,
            ILogger<TriggerWorkflowsRequestConsumer> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _bookmarkFinder = bookmarkFinder;
            _triggerFinder = triggerFinder;
            _commandSender = commandSender;
            _logger = logger;
        }

        public async Task Handle(TriggerWorkflowsRequest message)
        {
            var correlationId = message.CorrelationId;
            
            // Find correlated workflows.
            var correlatedWorkflowInstances = await _workflowInstanceStore.FindManyAsync(new CorrelationIdSpecification<WorkflowInstance>(correlationId)).ToList();

            if (correlatedWorkflowInstances.Count > 0)
            {
                _logger.LogDebug("{WorkflowInstanceCount} existing workflows found with correlation ID '{CorrelationId}' will be queued for execution", correlatedWorkflowInstances.Count, correlationId);
                var correlatedWorkflowInstanceIds = correlatedWorkflowInstances.Select(x => x.Id).ToHashSet();
                var bookmarkFinderResults = await _bookmarkFinder.FindBookmarksAsync(message.ActivityType, message.Bookmark, message.TenantId).Where(x => correlatedWorkflowInstanceIds.Contains(x.WorkflowInstanceId)).ToList();
                await EnqueueWorkflowsAsync(bookmarkFinderResults, message.Input);
                return;
            }

            // No correlated workflows found, so go ahead and start new & resume existing workflows.
            _logger.LogDebug("No workflows found with correlation ID '{CorrelationId}'. Starting new and resuming existing workflows", correlationId);
            await StartWorkflowsAsync(message);
            await ResumeWorkflowsAsync(message);
        }

        private async Task StartWorkflowsAsync(TriggerWorkflowsRequest message)
        {
            var filter = message.Trigger;
            var results = await _triggerFinder.FindTriggersAsync(message.ActivityType, filter, message.TenantId);

            foreach (var result in results)
            {
                var workflowBlueprint = result.WorkflowBlueprint;
                await EnqueueWorkflowDefinition(workflowBlueprint.Id, workflowBlueprint.TenantId, result.ActivityId, message.Input, message.CorrelationId, message.ContextId);
            }
        }

        private async Task ResumeWorkflowsAsync(TriggerWorkflowsRequest message)
        {
            var filter = message.Bookmark;
            var results = await _bookmarkFinder.FindBookmarksAsync(message.ActivityType, filter, message.TenantId);
            await EnqueueWorkflowsAsync(results, message.Input);
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