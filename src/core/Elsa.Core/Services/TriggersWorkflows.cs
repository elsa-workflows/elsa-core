using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Triggers;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

namespace Elsa.Services
{
    public class TriggersWorkflows : ITriggersWorkflows
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly ITriggerFinder _triggerFinder;
        private readonly IStartsWorkflow _startsWorkflow;
        private readonly IResumesWorkflow _resumesWorkflow;
        private readonly ILogger _logger;

        public TriggersWorkflows(
            IWorkflowInstanceStore workflowInstanceStore,
            IBookmarkFinder bookmarkFinder,
            ITriggerFinder triggerFinder,
            IStartsWorkflow startsWorkflow,
            IResumesWorkflow resumesWorkflow,
            ILogger<TriggersWorkflows> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _bookmarkFinder = bookmarkFinder;
            _triggerFinder = triggerFinder;
            _startsWorkflow = startsWorkflow;
            _resumesWorkflow = resumesWorkflow;
            _logger = logger;
        }

        public async Task<IEnumerable<WorkflowInstance>> TriggerWorkflowsAsync(
            string activityType,
            IBookmark bookmark,
            IBookmark trigger,
            string? correlationId,
            object? input = default,
            string? contextId = default,
            string? tenantId = default,
            CancellationToken cancellationToken = default)
        {
            // Find correlated workflows.
            var correlatedWorkflowInstances = await _workflowInstanceStore.FindManyAsync(new CorrelationIdSpecification<WorkflowInstance>(correlationId), cancellationToken: cancellationToken).ToList();

            if (correlatedWorkflowInstances.Count > 0)
            {
                _logger.LogDebug("{WorkflowInstanceCount} existing workflows found with correlation ID '{CorrelationId}' will be queued for execution", correlatedWorkflowInstances.Count, correlationId);
                var correlatedWorkflowInstanceIds = correlatedWorkflowInstances.Select(x => x.Id).ToHashSet();
                var bookmarkFinderResults = await _bookmarkFinder.FindBookmarksAsync(activityType, bookmark, tenantId, cancellationToken).Where(x => correlatedWorkflowInstanceIds.Contains(x.WorkflowInstanceId)).ToList();
                return await ResumeWorkflowsAsync(bookmarkFinderResults, input, cancellationToken).ToListAsync(cancellationToken);
            }

            // No correlated workflows found, so go ahead and start new & resume existing workflows. 
            _logger.LogDebug("No workflows found with correlation ID '{CorrelationId}'. Starting new and resuming existing workflows", correlationId);
            return await TriggerWorkflowsInternalAsync(activityType, bookmark, trigger, correlationId, input, contextId, tenantId, cancellationToken).ToList();
        }

        public async Task<IEnumerable<WorkflowInstance>> TriggerWorkflowsInternalAsync(
            string activityType,
            IBookmark bookmark,
            IBookmark trigger,
            string? correlationId,
            object? input = default,
            string? contextId = default,
            string? tenantId = default,
            CancellationToken cancellationToken = default)
        {
            var startedWorkflowInstances = await StartWorkflowsAsync(activityType, trigger, input, correlationId, contextId, tenantId, cancellationToken).ToListAsync(cancellationToken);
            var resumedWorkflowInstances = await ResumeWorkflowsAsync(activityType, bookmark, input, tenantId, cancellationToken).ToList();
            var workflowInstances = startedWorkflowInstances.Concat(resumedWorkflowInstances);

            return workflowInstances;
        }

        private async IAsyncEnumerable<WorkflowInstance> StartWorkflowsAsync(
            string activityType,
            IBookmark trigger,
            object? input,
            string? correlationId,
            string? contextId,
            string? tenantId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var triggerResults = (await _triggerFinder.FindTriggersAsync(activityType, trigger, tenantId, cancellationToken)).ToList();

            foreach (var result in triggerResults)
            {
                var workflowBlueprint = result.WorkflowBlueprint;
                var activityId = result.ActivityId;
                var workflowInstance = await _startsWorkflow.StartWorkflowAsync(workflowBlueprint, activityId, input, correlationId, contextId, cancellationToken);
                yield return workflowInstance;
            }
        }

        private async Task<IEnumerable<WorkflowInstance>> ResumeWorkflowsAsync(string activityType, IBookmark bookmark, object? input, string? tenantId, CancellationToken cancellationToken)
        {
            var bookmarkResults = await _bookmarkFinder.FindBookmarksAsync(activityType, bookmark, tenantId, cancellationToken).ToList();
            return await ResumeWorkflowsAsync(bookmarkResults, input, cancellationToken).ToListAsync(cancellationToken);
        }

        private async IAsyncEnumerable<WorkflowInstance> ResumeWorkflowsAsync(IEnumerable<BookmarkFinderResult> bookmarkResults, object? input, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var bookmarkResult in bookmarkResults)
            {
                var workflowInstance = await _workflowInstanceStore.FindByIdAsync(bookmarkResult.WorkflowInstanceId, cancellationToken);

                if (workflowInstance == null)
                {
                    _logger.LogInformation("Bookmark {Bookmark} referenced workflow instance {WorkflowInstanceId} that no longer exists", bookmarkResult.Bookmark.GetType().Name, bookmarkResult.WorkflowInstanceId);
                    continue;
                }

                workflowInstance = await _resumesWorkflow.ResumeWorkflowAsync(workflowInstance, bookmarkResult.ActivityId, input, cancellationToken);
                yield return workflowInstance;
            }
        }
    }
}