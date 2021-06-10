using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Services.Bookmarks;
using Elsa.Services.Dispatch;
using Elsa.Services.Models;
using Elsa.Services.Triggers;
using Medallion.Threading;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

namespace Elsa.Services.Workflows
{
    public class WorkflowLaunchpad : IWorkflowLaunchpad
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowFactory _workflowFactory;
        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly ITriggerFinder _triggerFinder;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly IWorkflowInstanceDispatcher _workflowInstanceDispatcher;
        private readonly IWorkflowInstanceExecutor _workflowInstanceExecutor;
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IGetsStartActivities _getsStartActivities;
        private readonly ElsaOptions _elsaOptions;
        private readonly ILogger _logger;

        public WorkflowLaunchpad(
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowFactory workflowFactory,
            IBookmarkFinder bookmarkFinder,
            ITriggerFinder triggerFinder,
            IDistributedLockProvider distributedLockProvider,
            IWorkflowInstanceDispatcher workflowInstanceDispatcher,
            IWorkflowInstanceExecutor workflowInstanceExecutor,
            IWorkflowRunner workflowRunner,
            IWorkflowRegistry workflowRegistry,
            IGetsStartActivities getsStartActivities,
            ElsaOptions elsaOptions,
            ILogger<WorkflowLaunchpad> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _bookmarkFinder = bookmarkFinder;
            _triggerFinder = triggerFinder;
            _distributedLockProvider = distributedLockProvider;
            _workflowInstanceDispatcher = workflowInstanceDispatcher;
            _elsaOptions = elsaOptions;
            _logger = logger;
            _getsStartActivities = getsStartActivities;
            _workflowRegistry = workflowRegistry;
            _workflowRunner = workflowRunner;
            _workflowInstanceExecutor = workflowInstanceExecutor;
            _workflowFactory = workflowFactory;
        }

        public async Task<IEnumerable<PendingWorkflow>> CollectWorkflowsAsync(CollectWorkflowsContext context, CancellationToken cancellationToken = default)
        {
            var correlationId = context.CorrelationId;

            if (!string.IsNullOrWhiteSpace(correlationId))
                return await CollectResumableOrStartableCorrelatedWorkflowsAsync(context, cancellationToken);

            if (!string.IsNullOrWhiteSpace(context.WorkflowInstanceId))
                return await CollectSpecificWorkflowInstanceAsync(context, cancellationToken);

            return await CollectResumableAndStartableWorkflowsAsync(context, cancellationToken);
        }

        public async Task<IEnumerable<StartableWorkflow>> CollectStartableWorkflowsAsync(CollectWorkflowsContext context, CancellationToken cancellationToken = default)
        {
            var correlationId = context.CorrelationId ?? Guid.NewGuid().ToString("N");
            var updatedContext = context with {CorrelationId = correlationId};
            await using var lockHandle = await AcquireLockAsync(correlationId, cancellationToken);
            return await CollectStartableWorkflowsInternalAsync(updatedContext, cancellationToken);
        }

        public async Task<StartableWorkflow?> CollectStartableWorkflowAsync(string workflowDefinitionId, string? activityId, string? correlationId = default, string? contextId = default, string? tenantId = default,
            CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = await _workflowRegistry.GetAsync(workflowDefinitionId, tenantId, VersionOptions.Published, cancellationToken);

            if (workflowBlueprint == null)
                return null;

            return await CollectStartableWorkflowAsync(workflowBlueprint, activityId, correlationId, contextId, tenantId, cancellationToken);
        }

        public async Task<StartableWorkflow?> CollectStartableWorkflowAsync(
            IWorkflowBlueprint workflowBlueprint, 
            string? activityId, 
            string? correlationId = default, 
            string? contextId = default, 
            string? tenantId = default,
            CancellationToken cancellationToken = default)
        {
            correlationId ??= Guid.NewGuid().ToString("N");
            
            // Acquire a lock on correlation ID to prevent duplicate workflow instances from being created.
            await using var correlationLockHandle = await AcquireLockAsync(correlationId, cancellationToken);

            return await CollectStartableWorkflowInternalAsync(workflowBlueprint, activityId, correlationId, contextId, tenantId, cancellationToken);
        }

        public async Task CollectAndExecuteStartableWorkflowAsync(string workflowDefinitionId, string? activityId, string? correlationId = default, string? contextId = default, object? input = default, string? tenantId = default,
            CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = await _workflowRegistry.GetAsync(workflowDefinitionId, tenantId, VersionOptions.Published, cancellationToken);

            if (workflowBlueprint == null)
            {
                _logger.LogWarning("Could not find workflow with ID {WorkflowDefinitionId}", workflowDefinitionId);
                return;
            }

            await CollectAndExecuteStartableWorkflowAsync(workflowBlueprint, activityId, correlationId, contextId, input, cancellationToken);
        }

        public async Task<RunWorkflowResult> CollectAndExecuteStartableWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string? activityId, string? correlationId = default, string? contextId = default, object? input = default,
            CancellationToken cancellationToken = default)
        {
            var workflowDefinitionId = workflowBlueprint.Id;
            var tenantId = workflowBlueprint.TenantId;

            var startableWorkflow = await CollectStartableWorkflowAsync(workflowBlueprint, activityId, correlationId, contextId, tenantId, cancellationToken);

            if (startableWorkflow == null)
                throw new WorkflowException($"Could not start workflow with ID {workflowDefinitionId}");

            return await ExecuteStartableWorkflowAsync(startableWorkflow, input, cancellationToken);
        }

        public async Task ExecutePendingWorkflowsAsync(IEnumerable<PendingWorkflow> pendingWorkflows, object? input = default, CancellationToken cancellationToken = default)
        {
            foreach (var pendingWorkflow in pendingWorkflows)
                await _workflowInstanceExecutor.ExecuteAsync(pendingWorkflow.WorkflowInstanceId, pendingWorkflow.ActivityId, input, cancellationToken);
        }

        public async Task ExecutePendingWorkflowAsync(PendingWorkflow pendingWorkflow, object? input = default, CancellationToken cancellationToken = default) =>
            await _workflowInstanceExecutor.ExecuteAsync(pendingWorkflow.WorkflowInstanceId, pendingWorkflow.ActivityId, input, cancellationToken);

        public async Task<RunWorkflowResult> ExecutePendingWorkflowAsync(string workflowInstanceId, string? activityId, object? input = default, CancellationToken cancellationToken = default) =>
            await _workflowInstanceExecutor.ExecuteAsync(workflowInstanceId, activityId, input, cancellationToken);

        public async Task DispatchPendingWorkflowsAsync(IEnumerable<PendingWorkflow> pendingWorkflows, object? input, CancellationToken cancellationToken = default)
        {
            foreach (var pendingWorkflow in pendingWorkflows)
                await DispatchPendingWorkflowAsync(pendingWorkflow, input, cancellationToken);
        }

        public async Task DispatchPendingWorkflowAsync(PendingWorkflow pendingWorkflow, object? input, CancellationToken cancellationToken = default) =>
            await _workflowInstanceDispatcher.DispatchAsync(new ExecuteWorkflowInstanceRequest(pendingWorkflow.WorkflowInstanceId, pendingWorkflow.ActivityId, input), cancellationToken);

        public Task DispatchPendingWorkflowAsync(string workflowInstanceId, string? activityId, object? input, CancellationToken cancellationToken = default) =>
            DispatchPendingWorkflowAsync(new PendingWorkflow(workflowInstanceId, activityId), input, cancellationToken);

        public async Task<RunWorkflowResult> ExecuteStartableWorkflowAsync(StartableWorkflow startableWorkflow, object? input, CancellationToken cancellationToken = default) =>
            await _workflowRunner.RunWorkflowAsync(startableWorkflow.WorkflowBlueprint, startableWorkflow.WorkflowInstance, startableWorkflow.ActivityId, input, cancellationToken);

        public async Task<PendingWorkflow> DispatchStartableWorkflowAsync(StartableWorkflow startableWorkflow, object? input, CancellationToken cancellationToken = default)
        {
            var pendingWorkflow = new PendingWorkflow(startableWorkflow.WorkflowInstance.Id, startableWorkflow.ActivityId);
            await ExecutePendingWorkflowAsync(pendingWorkflow, input, cancellationToken);
            return pendingWorkflow;
        }

        public async Task<IEnumerable<StartedWorkflow>> CollectAndExecuteWorkflowsAsync(CollectWorkflowsContext context, object? input = default, CancellationToken cancellationToken = default)
        {
            var pendingWorkflows = await CollectWorkflowsAsync(context, cancellationToken).ToList();
            await ExecutePendingWorkflowsAsync(pendingWorkflows, input, cancellationToken);
            return pendingWorkflows.Select(x => new StartedWorkflow(x.WorkflowInstanceId, x.ActivityId));
        }

        public async Task<IEnumerable<PendingWorkflow>> CollectAndDispatchWorkflowsAsync(CollectWorkflowsContext context, object? input = default, CancellationToken cancellationToken = default)
        {
            var pendingWorkflows = await CollectWorkflowsAsync(context, cancellationToken).ToList();
            await DispatchPendingWorkflowsAsync(pendingWorkflows, input, cancellationToken);
            return pendingWorkflows;
        }

        public async Task<IEnumerable<PendingWorkflow>> CollectResumableAndStartableWorkflowsAsync(CollectWorkflowsContext context, CancellationToken cancellationToken)
        {
            var bookmarkResultsQuery = context.Bookmark != null ? await _bookmarkFinder.FindBookmarksAsync(context.ActivityType, context.Bookmark, context.CorrelationId, context.TenantId, cancellationToken) : default;
            var bookmarkResults = bookmarkResultsQuery?.ToList() ?? new List<BookmarkFinderResult>();
            var triggeredPendingWorkflows = bookmarkResults.Select(x => new PendingWorkflow(x.WorkflowInstanceId, x.ActivityId)).ToList();
            var startableWorkflows = await CollectStartableWorkflowsAsync(context, cancellationToken);
            var pendingWorkflows = triggeredPendingWorkflows.Concat(startableWorkflows.Select(x => new PendingWorkflow(x.WorkflowInstance.Id, x.ActivityId))).Distinct().ToList();

            return pendingWorkflows;
        }
        
        private async Task<IEnumerable<StartableWorkflow>> CollectStartableWorkflowsInternalAsync(CollectWorkflowsContext context, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Triggering workflows using {ActivityType}", context.ActivityType);

            var filter = context.Trigger;
            var triggers = filter != null ? (await _triggerFinder.FindTriggersAsync(context.ActivityType, filter, context.TenantId, cancellationToken)).ToList() : new List<TriggerFinderResult>();
            var startableWorkflows = new List<StartableWorkflow>();

            foreach (var trigger in triggers)
            {
                var workflowBlueprint = trigger.WorkflowBlueprint;
                var startableWorkflow = await CollectStartableWorkflowInternalAsync(workflowBlueprint, trigger.ActivityId, context.CorrelationId!, context.ContextId, context.TenantId, cancellationToken);

                if (startableWorkflow != null)
                    startableWorkflows.Add(startableWorkflow);
            }

            return startableWorkflows;
        }

        private async Task<StartableWorkflow?> CollectStartableWorkflowInternalAsync(
            IWorkflowBlueprint workflowBlueprint, 
            string? activityId, 
            string correlationId, 
            string? contextId = default, 
            string? tenantId = default,
            CancellationToken cancellationToken = default)
        {
            var workflowDefinitionId = workflowBlueprint.Id;

            if (!ValidatePreconditions(workflowDefinitionId, workflowBlueprint))
                return null;

            // Acquire a lock on the workflow definition so that we can ensure singleton-workflows never execute more than one instance. 
            var lockKey = $"execute-workflow-definition:tenant:{tenantId}:workflow-definition:{workflowDefinitionId}";
            await using var workflowDefinitionHandle = await AcquireLockAsync(lockKey, cancellationToken);

            if (workflowBlueprint.IsSingleton)
            {
                if (await GetWorkflowIsAlreadyExecutingAsync(tenantId, workflowDefinitionId))
                {
                    _logger.LogDebug("Workflow {WorkflowDefinitionId} is a singleton workflow and is already running", workflowDefinitionId);
                    return null;
                }
            }

            var startActivities = _getsStartActivities.GetStartActivities(workflowBlueprint).Select(x => x.Id).ToHashSet();
            var startActivityId = activityId == null ? startActivities.FirstOrDefault() : startActivities.Contains(activityId) ? activityId : default;

            if (startActivityId == null)
            {
                _logger.LogWarning("Cannot start workflow {WorkflowDefinitionId} with version {WorkflowDefinitionVersion} because it has no starting activities", workflowBlueprint.Id, workflowBlueprint.Version);
                return null;
            }

            var workflowInstance = await _workflowFactory.InstantiateAsync(
                workflowBlueprint,
                correlationId,
                contextId,
                cancellationToken);

            await _workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
            return new StartableWorkflow(workflowBlueprint, workflowInstance, startActivityId);
        }

        private async Task<IEnumerable<PendingWorkflow>> CollectSpecificWorkflowInstanceAsync(CollectWorkflowsContext context, CancellationToken cancellationToken)
        {
            var bookmarkResultsQuery = context.Bookmark != null ? await _bookmarkFinder.FindBookmarksAsync(context.ActivityType, context.Bookmark, context.CorrelationId, context.TenantId, cancellationToken) : default;
            bookmarkResultsQuery = bookmarkResultsQuery?.Where(x => x.WorkflowInstanceId == context.WorkflowInstanceId);
            var bookmarkResults = bookmarkResultsQuery?.ToList() ?? new List<BookmarkFinderResult>();
            var pendingWorkflows = bookmarkResults.Select(x => new PendingWorkflow(x.WorkflowInstanceId, x.ActivityId)).ToList();

            return pendingWorkflows;
        }

        private async Task<IEnumerable<PendingWorkflow>> CollectResumableOrStartableCorrelatedWorkflowsAsync(CollectWorkflowsContext context, CancellationToken cancellationToken)
        {
            var correlationId = context.CorrelationId!;

            await using var handle = await AcquireLockAsync(correlationId, cancellationToken);
            var correlatedWorkflowInstanceCount = !string.IsNullOrWhiteSpace(correlationId)
                ? await _workflowInstanceStore.CountAsync(new CorrelationIdSpecification<WorkflowInstance>(correlationId).And(new WorkflowUnfinishedStatusSpecification()), cancellationToken)
                : 0;

            _logger.LogDebug("Found {CorrelatedWorkflowCount} workflows with correlation ID {CorrelationId}", correlatedWorkflowInstanceCount, correlationId);

            if (correlatedWorkflowInstanceCount > 0)
            {
                var bookmarkResults = context.Bookmark != null
                    ? await _bookmarkFinder.FindBookmarksAsync(context.ActivityType, context.Bookmark, correlationId, context.TenantId, cancellationToken).ToList()
                    : new List<BookmarkFinderResult>();
                _logger.LogDebug("Found {BookmarkCount} bookmarks for activity type {ActivityType}", bookmarkResults.Count, context.ActivityType);
                return bookmarkResults.Select(x => new PendingWorkflow(x.WorkflowInstanceId, x.ActivityId)).ToList();
            }

            var startableWorkflows = await CollectStartableWorkflowsInternalAsync(context, cancellationToken);
            return startableWorkflows.Select(x => new PendingWorkflow(x.WorkflowInstance.Id, x.ActivityId)).ToList();
        }

        private async Task<IDistributedSynchronizationHandle> AcquireLockAsync(string resource, CancellationToken cancellationToken)
        {
            var handle = await _distributedLockProvider.AcquireLockAsync(resource, _elsaOptions.DistributedLockTimeout, cancellationToken);

            if (handle == null)
                throw new LockAcquisitionException($"Failed to acquire a lock on {resource}");

            return handle;
        }

        private bool ValidatePreconditions(string? workflowDefinitionId, IWorkflowBlueprint? workflowBlueprint)
        {
            if (workflowBlueprint != null)
                return true;

            _logger.LogWarning("No workflow definition {WorkflowDefinitionId} found. Make sure the scheduled workflow definition is published and enabled", workflowDefinitionId);
            return false;
        }

        private async Task<bool> GetWorkflowIsAlreadyExecutingAsync(string? tenantId, string workflowDefinitionId)
        {
            var specification = new TenantSpecification<WorkflowInstance>(tenantId).WithWorkflowDefinition(workflowDefinitionId).And(new WorkflowIsAlreadyExecutingSpecification());
            return await _workflowInstanceStore.FindAsync(specification) != null;
        }
    }
}