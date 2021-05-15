using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Elsa.Exceptions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Services;
using Elsa.Triggers;
using MediatR;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

namespace Elsa.Dispatch.Handlers
{
    public class TriggerWorkflows : IRequestHandler<TriggerWorkflowsRequest, TriggerWorkflowsResponse>
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly ITriggerFinder _triggerFinder;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly IMediator _mediator;
        private readonly ElsaOptions _elsaOptions;
        private readonly ILogger<TriggerWorkflows> _logger;

        public TriggerWorkflows(
            IWorkflowInstanceStore workflowInstanceStore,
            IBookmarkFinder bookmarkFinder,
            ITriggerFinder triggerFinder,
            IDistributedLockProvider distributedLockProvider,
            IMediator mediator,
            ElsaOptions elsaOptions,
            ILogger<TriggerWorkflows> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _bookmarkFinder = bookmarkFinder;
            _triggerFinder = triggerFinder;
            _distributedLockProvider = distributedLockProvider;
            _mediator = mediator;
            _elsaOptions = elsaOptions;
            _logger = logger;
        }

        public async Task<TriggerWorkflowsResponse> Handle(TriggerWorkflowsRequest request, CancellationToken cancellationToken)
        {
            var correlationId = request.CorrelationId;

            if (!string.IsNullOrWhiteSpace(correlationId))
                return await ResumeOrStartCorrelatedWorkflowsAsync(request, cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.WorkflowInstanceId))
                return await ResumeSpecificWorkflowInstanceAsync(request, cancellationToken);

            return await TriggerWorkflowsAsync(request, cancellationToken);
        }

        private async Task<TriggerWorkflowsResponse> TriggerWorkflowsAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken)
        {
            var bookmarkResultsQuery = await _bookmarkFinder.FindBookmarksAsync(request.ActivityType, request.Bookmark, request.CorrelationId, request.TenantId, cancellationToken);
            var bookmarkResults = bookmarkResultsQuery.ToList();
            var triggeredPendingWorkflows = bookmarkResults.Select(x => new PendingWorkflow(x.WorkflowInstanceId, x.ActivityId)).ToList();
            await ResumeWorkflowsAsync(bookmarkResults, request.Input, request.Execute, cancellationToken);
            var startWorkflowsResponse = await StartWorkflowsAsync(request, cancellationToken);
            var pendingWorkflows = triggeredPendingWorkflows.Concat(startWorkflowsResponse.PendingWorkflows).Distinct().ToList();

            return new TriggerWorkflowsResponse(pendingWorkflows);
        }

        private async Task<TriggerWorkflowsResponse> ResumeSpecificWorkflowInstanceAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken)
        {
            var bookmarkResultsQuery = await _bookmarkFinder.FindBookmarksAsync(request.ActivityType, request.Bookmark, request.CorrelationId, request.TenantId, cancellationToken);
            bookmarkResultsQuery = bookmarkResultsQuery.Where(x => x.WorkflowInstanceId == request.WorkflowInstanceId);
            var bookmarkResults = bookmarkResultsQuery.ToList();
            await ResumeWorkflowsAsync(bookmarkResults, request.Input, request.Execute, cancellationToken);
            var pendingWorkflows = bookmarkResults.Select(x => new PendingWorkflow(x.WorkflowInstanceId, x.ActivityId)).ToList();

            return new TriggerWorkflowsResponse(pendingWorkflows);
        }

        private async Task<TriggerWorkflowsResponse> ResumeOrStartCorrelatedWorkflowsAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken)
        {
            var correlationId = request.CorrelationId!;
            var lockKey = correlationId;

            _logger.LogDebug("Acquiring lock on correlation ID {CorrelationId}", correlationId);
            await using (var handle = await _distributedLockProvider.AcquireLockAsync(lockKey, _elsaOptions.DistributedLockTimeout, cancellationToken))
            {
                if (handle == null)
                    throw new LockAcquisitionException($"Failed to acquire a lock on {lockKey}");

                var correlatedWorkflowInstanceCount = !string.IsNullOrWhiteSpace(correlationId)
                    ? await _workflowInstanceStore.CountAsync(new CorrelationIdSpecification<WorkflowInstance>(correlationId).WithStatus(WorkflowStatus.Suspended), cancellationToken)
                    : 0;

                _logger.LogDebug("Found {CorrelatedWorkflowCount} correlated workflows,", correlatedWorkflowInstanceCount);

                if (correlatedWorkflowInstanceCount > 0)
                {
                    _logger.LogDebug("{WorkflowInstanceCount} existing workflows found with correlation ID '{CorrelationId}' will be queued for execution", correlatedWorkflowInstanceCount, correlationId);
                    var bookmarkResults = await _bookmarkFinder.FindBookmarksAsync(request.ActivityType, request.Bookmark, correlationId, request.TenantId, cancellationToken).ToList();
                    await ResumeWorkflowsAsync(bookmarkResults, request.Input, request.Execute, cancellationToken);
                    return new TriggerWorkflowsResponse(bookmarkResults.Select(x => new PendingWorkflow(x.WorkflowInstanceId, x.ActivityId)).ToList());
                }
            }

            return await StartWorkflowsAsync(request, cancellationToken);
        }

        private async Task<TriggerWorkflowsResponse> StartWorkflowsAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Triggering workflows using {ActivityType}", request.ActivityType);

            var filter = request.Trigger;
            var triggers = (await _triggerFinder.FindTriggersAsync(request.ActivityType, filter, request.TenantId, cancellationToken)).ToList();
            var pendingWorkflows = new List<PendingWorkflow>();

            foreach (var trigger in triggers)
            {
                var workflowBlueprint = trigger.WorkflowBlueprint;
                var response = await _mediator.Send(new ExecuteWorkflowDefinitionRequest(workflowBlueprint.Id, trigger.ActivityId, request.Input, request.CorrelationId, request.ContextId, workflowBlueprint.TenantId, request.Execute), cancellationToken);

                if (response.PendingWorkflow != null)
                    pendingWorkflows.Add(response.PendingWorkflow);
            }

            return new TriggerWorkflowsResponse(pendingWorkflows);
        }

        private async Task ResumeWorkflowsAsync(IEnumerable<BookmarkFinderResult> results, object? input, bool execute, CancellationToken cancellationToken)
        {
            foreach (var result in results)
                await _mediator.Send(new ExecuteWorkflowInstanceRequest(result.WorkflowInstanceId, result.ActivityId, input, execute), cancellationToken);
        }
    }
}