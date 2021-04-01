using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Elsa.Dispatch;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Server.Orleans.Grains.Contracts;
using Elsa.Triggers;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;
using Orleans;
using Orleans.Concurrency;

namespace Elsa.Server.Orleans.Grains
{
    [StatelessWorker]
    public class CorrelatedWorkflowDefinitionGrain : Grain, ICorrelatedWorkflowGrain
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly ITriggerFinder _triggerFinder;
        private readonly IWorkflowDefinitionDispatcher _workflowDefinitionDispatcher;
        private readonly IWorkflowInstanceDispatcher _workflowInstanceDispatcher;
        private readonly ILogger<CorrelatedWorkflowDefinitionGrain> _logger;

        public CorrelatedWorkflowDefinitionGrain(
            IWorkflowInstanceStore workflowInstanceStore,
            IBookmarkFinder bookmarkFinder,
            ITriggerFinder triggerFinder,
            IWorkflowDefinitionDispatcher workflowDefinitionDispatcher,
            IWorkflowInstanceDispatcher workflowInstanceDispatcher,
            ILogger<CorrelatedWorkflowDefinitionGrain> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _bookmarkFinder = bookmarkFinder;
            _triggerFinder = triggerFinder;
            _workflowDefinitionDispatcher = workflowDefinitionDispatcher;
            _workflowInstanceDispatcher = workflowInstanceDispatcher;
            _logger = logger;
        }

        public async Task ExecutedCorrelatedWorkflowAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken = default)
        {
            var correlationId = request.CorrelationId;
            var correlatedWorkflowInstanceCount = await _workflowInstanceStore.CountAsync(new CorrelationIdSpecification<WorkflowInstance>(correlationId), cancellationToken);

            if (correlatedWorkflowInstanceCount > 0)
            {
                _logger.LogDebug("{WorkflowInstanceCount} existing workflows found with correlation ID '{CorrelationId}' will be queued for execution", correlatedWorkflowInstanceCount, correlationId);
                var existingWorkflows = await _bookmarkFinder.FindBookmarksAsync(request.ActivityType, request.Bookmark, request.TenantId, cancellationToken).ToList();
                await ResumeWorkflowsAsync(existingWorkflows, request.Input, cancellationToken);
            }
            else
            {
                _logger.LogDebug("No existing workflows found with correlation ID '{CorrelationId}'. Starting new workflow", correlationId);
                await StartWorkflowsAsync(request, cancellationToken);
            }
        }

        private async Task StartWorkflowsAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken)
        {
            var filter = request.Trigger;
            var triggers = await _triggerFinder.FindTriggersAsync(request.ActivityType, filter, request.TenantId, cancellationToken);

            foreach (var trigger in triggers)
            {
                var workflowBlueprint = trigger.WorkflowBlueprint;
                await _workflowDefinitionDispatcher.DispatchAsync(new ExecuteWorkflowDefinitionRequest(workflowBlueprint.Id, trigger.ActivityId, request.Input, request.CorrelationId, request.ContextId, workflowBlueprint.TenantId), cancellationToken);
            }
        }

        private async Task ResumeWorkflowsAsync(IEnumerable<BookmarkFinderResult> results, object? input, CancellationToken cancellationToken)
        {
            foreach (var result in results)
                await _workflowInstanceDispatcher.DispatchAsync(new ExecuteWorkflowInstanceRequest(result.WorkflowInstanceId, result.ActivityId, input), cancellationToken);
        }
    }
}