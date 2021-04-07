using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Elsa.Exceptions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Elsa.Triggers;
using MediatR;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

namespace Elsa.Dispatch.Handlers
{
    public class TriggerWorkflows : IRequestHandler<TriggerWorkflowsRequest, int>
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly ITriggerFinder _triggerFinder;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly IWorkflowDefinitionDispatcher _workflowDefinitionDispatcher;
        private readonly IWorkflowInstanceDispatcher _workflowInstanceDispatcher;
        private readonly IMediator _mediator;
        private readonly ElsaOptions _elsaOptions;
        private readonly ILogger<TriggerWorkflows> _logger;

        public TriggerWorkflows(
            IWorkflowInstanceStore workflowInstanceStore,
            IBookmarkFinder bookmarkFinder,
            ITriggerFinder triggerFinder,
            IDistributedLockProvider distributedLockProvider,
            IWorkflowDefinitionDispatcher workflowDefinitionDispatcher,
            IWorkflowInstanceDispatcher workflowInstanceDispatcher,
            ElsaOptions elsaOptions,
            ILogger<TriggerWorkflows> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _bookmarkFinder = bookmarkFinder;
            _triggerFinder = triggerFinder;
            _distributedLockProvider = distributedLockProvider;
            _workflowDefinitionDispatcher = workflowDefinitionDispatcher;
            _workflowInstanceDispatcher = workflowInstanceDispatcher;
            _elsaOptions = elsaOptions;
            _logger = logger;
        }

        public async Task<int> Handle(TriggerWorkflowsRequest request, CancellationToken cancellationToken)
        {
            var correlationId = request.CorrelationId;

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                var lockKey = $"trigger-workflows:correlation:{correlationId}";

                await using var handle = await _distributedLockProvider.AcquireLockAsync(lockKey, _elsaOptions.DistributedLockTimeout, cancellationToken);
                
                if(handle == null)
                    throw new LockAcquisitionException($"Failed to acquire a lock on {lockKey}");

                var correlatedWorkflowInstanceCount = !string.IsNullOrWhiteSpace(correlationId) ? await _workflowInstanceStore.CountAsync(new CorrelationIdSpecification<WorkflowInstance>(correlationId), cancellationToken) : 0;

                // Release lock before executing workflows. If we don't, we potentially enter a deadlock. 
                await handle.DisposeAsync();
                
                if (correlatedWorkflowInstanceCount > 0)
                {
                    _logger.LogDebug("{WorkflowInstanceCount} existing workflows found with correlation ID '{CorrelationId}' will be queued for execution", correlatedWorkflowInstanceCount, correlationId);
                    var bookmarkResults = await _bookmarkFinder.FindBookmarksAsync(request.ActivityType, request.Bookmark, request.TenantId, cancellationToken).ToList();
                    await ResumeWorkflowsAsync(bookmarkResults, request.Input, cancellationToken);
                    return correlatedWorkflowInstanceCount;
                }
            }

            return await StartWorkflowsAsync(request, cancellationToken);
        }

        private async Task<int> StartWorkflowsAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Triggering workflows using {ActivityType}", request.ActivityType);

            var filter = request.Trigger;
            var triggers = (await _triggerFinder.FindTriggersAsync(request.ActivityType, filter, request.TenantId, cancellationToken)).ToList();

            foreach (var trigger in triggers)
            {
                var workflowBlueprint = trigger.WorkflowBlueprint;
                
                await _workflowDefinitionDispatcher.DispatchAsync(new ExecuteWorkflowDefinitionRequest(workflowBlueprint.Id, trigger.ActivityId, request.Input, request.CorrelationId, request.ContextId, workflowBlueprint.TenantId),
                    cancellationToken);
            }

            return triggers.Count;
        }

        private async Task ResumeWorkflowsAsync(IEnumerable<BookmarkFinderResult> results, object? input, CancellationToken cancellationToken)
        {
            foreach (var result in results)
                await _workflowInstanceDispatcher.DispatchAsync(new ExecuteWorkflowInstanceRequest(result.WorkflowInstanceId, result.ActivityId, input), cancellationToken);
        }
    }
}