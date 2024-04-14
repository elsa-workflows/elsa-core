using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Requests;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Matches;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.State;
using Medallion.Threading;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// A default implementation of <see cref="IWorkflowRuntime"/> that relies on persistent storage and distributed locking for workflow execution.
/// </summary>
public class DefaultWorkflowRuntime(
    IWorkflowHostFactory workflowHostFactory,
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowInstanceStore workflowInstanceStore,
    IWorkflowInstanceManager workflowInstanceManager,
    ITriggerStore triggerStore,
    IBookmarkStore bookmarkStore,
    IBookmarkHasher hasher,
    IDistributedLockProvider distributedLockProvider,
    IWorkflowInstanceFactory workflowInstanceFactory,
    WorkflowStateMapper workflowStateMapper,
    IIdentityGenerator identityGenerator,
    IWorkflowExecutionContextStore workflowExecutionContextStore,
    IServiceProvider serviceProvider,
    IBookmarksPersister bookmarksPersister,
    ILogger<DefaultWorkflowRuntime> logger)
    : IWorkflowRuntime
{
    /// <inheritdoc />
    public async Task<CanStartWorkflowResult> CanStartWorkflowAsync(string definitionId, StartWorkflowRuntimeParams? @params = default)
    {
        var input = @params?.Input;
        var correlationId = @params?.CorrelationId;
        var workflowHost = await CreateWorkflowHostAsync(definitionId, @params, @params?.CancellationToken ?? default);
        var startWorkflowOptions = new StartWorkflowRequest
        {
            CorrelationId = correlationId,
            Input = input,
            TriggerActivityId = @params?.TriggerActivityId
        };
        var canStart = await workflowHost.CanStartWorkflowAsync(startWorkflowOptions, @params?.CancellationToken ?? default);
        return new CanStartWorkflowResult(null, canStart);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult> StartWorkflowAsync(string definitionId, StartWorkflowRuntimeParams? @params = default)
    {
        var workflowHost = await CreateWorkflowHostAsync(definitionId, @params, @params?.CancellationToken ?? default);
        return await StartWorkflowAsync(workflowHost, @params);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult?> TryStartWorkflowAsync(string definitionId, StartWorkflowRuntimeParams? options = default)
    {
        return await StartWorkflowAsync(definitionId, options);
    }

    /// <inheritdoc />
    public async Task<ICollection<WorkflowExecutionResult>> StartWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options)
    {
        var results = new List<WorkflowExecutionResult>();
        var hash = hasher.Hash(activityTypeName, bookmarkPayload);
        var systemCancellationToken = options?.CancellationToken ?? default;

        // Start new workflows. This happens in a process-synchronized fashion to avoid multiple instances from being created. 
        var sharedResource = $"{nameof(DefaultWorkflowRuntime)}__StartTriggeredWorkflows__{hash}";

        await using (await AcquireLockAsync(sharedResource, systemCancellationToken))
        {
            var filter = new TriggerFilter
            {
                Hash = hash
            };
            var triggers = await triggerStore.FindManyAsync(filter, systemCancellationToken);

            foreach (var trigger in triggers)
            {
                var definitionId = trigger.WorkflowDefinitionId;

                var startOptions = new StartWorkflowRuntimeParams
                {
                    CorrelationId = options?.CorrelationId,
                    Input = options?.Input,
                    Properties = options?.Properties,
                    VersionOptions = VersionOptions.Published,
                    TriggerActivityId = trigger.ActivityId,
                    InstanceId = options?.WorkflowInstanceId,
                    CancellationToken = options?.CancellationToken ?? default
                };

                var canStartResult = await CanStartWorkflowAsync(definitionId, startOptions);

                // If we can't start the workflow, don't try it.
                if (!canStartResult.CanStart)
                    continue;

                var startResult = await StartWorkflowAsync(definitionId, startOptions);
                results.Add(startResult with
                {
                    TriggeredActivityId = trigger.ActivityId
                });
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult?> ResumeWorkflowAsync(string workflowInstanceId, ResumeWorkflowRuntimeParams? options = default)
    {
        var cancellationToken = options?.CancellationToken ?? default;

        await using (await AcquireLockAsync(workflowInstanceId, cancellationToken))
        {
            var workflowInstance = await workflowInstanceStore.FindAsync(new WorkflowInstanceFilter
            {
                Id = workflowInstanceId
            }, cancellationToken);

            if (workflowInstance == null)
                return null;

            var definitionId = workflowInstance.DefinitionId;
            var version = workflowInstance.Version;

            var workflow = await workflowDefinitionService.FindWorkflowAsync(
                definitionId,
                VersionOptions.SpecificVersion(version),
                cancellationToken);

            if (workflow == null)
                throw new Exception($"The workflow definition {definitionId} version {version} was not found");

            var workflowState = workflowStateMapper.Map(workflowInstance)!;
            var workflowHost = await workflowHostFactory.CreateAsync(workflow, workflowState, cancellationToken);

            var resumeWorkflowOptions = new ResumeWorkflowRequest
            {
                CorrelationId = options?.CorrelationId,
                BookmarkId = options?.BookmarkId,
                ActivityHandle = options?.ActivityHandle,
                Input = options?.Input,
                Properties = options?.Properties
            };

            await workflowHost.ExecuteWorkflowAsync(resumeWorkflowOptions, cancellationToken);
            workflowState = workflowHost.WorkflowState;
            return new WorkflowExecutionResult(workflowState.Id, workflowState.Status, workflowState.SubStatus, workflowState.Bookmarks, workflowState.Incidents);
        }
    }

    /// <inheritdoc />
    public async Task<ICollection<WorkflowExecutionResult>> ResumeWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options)
    {
        var hash = hasher.Hash(activityTypeName, bookmarkPayload, options?.ActivityInstanceId);
        var correlationId = options?.CorrelationId;
        var workflowInstanceId = options?.WorkflowInstanceId;
        var activityInstanceId = options?.ActivityInstanceId;
        var filter = new BookmarkFilter
        {
            Hash = hash,
            CorrelationId = correlationId,
            WorkflowInstanceId = workflowInstanceId,
            ActivityInstanceId = activityInstanceId
        };
        var bookmarks = await bookmarkStore.FindManyAsync(filter, options?.CancellationToken ?? default);

        return await ResumeWorkflowsAsync(
            bookmarks,
            new ResumeWorkflowRuntimeParams
            {
                CorrelationId = correlationId,
                Input = options?.Input,
                CancellationToken = options?.CancellationToken ?? default
            });
    }

    /// <inheritdoc />
    public async Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options)
    {
        var startedWorkflows = await StartWorkflowsAsync(activityTypeName, bookmarkPayload, options);
        var resumedWorkflows = await ResumeWorkflowsAsync(activityTypeName, bookmarkPayload, options);
        var results = startedWorkflows.Concat(resumedWorkflows).ToList();
        return new TriggerWorkflowsResult(results);
    }

    /// <inheritdoc />
    public Task<WorkflowExecutionResult> ExecuteWorkflowAsync(WorkflowMatch match, CancellationToken cancellationToken = default)
    {
        return ExecuteWorkflowAsync(match, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(WorkflowMatch match, ExecuteWorkflowRequest? options = default, CancellationToken cancellationToken = default)
    {
        if (match is StartableWorkflowMatch collectedStartableWorkflow)
        {
            var startOptions = new StartWorkflowRuntimeParams
            {
                CorrelationId = collectedStartableWorkflow.CorrelationId,
                Input = options?.Input,
                Properties = options?.Properties,
                VersionOptions = VersionOptions.Published,
                TriggerActivityId = collectedStartableWorkflow.ActivityId,
                InstanceId = collectedStartableWorkflow.WorkflowInstanceId,
                CancellationToken = cancellationToken
            };

            var startResult = await StartWorkflowAsync(collectedStartableWorkflow.DefinitionId!, startOptions);
            return startResult with
            {
                TriggeredActivityId = collectedStartableWorkflow.ActivityId
            };
        }

        var collectedResumableWorkflow = (match as ResumableWorkflowMatch)!;
        var runtimeOptions = new ResumeWorkflowRuntimeParams
        {
            CorrelationId = collectedResumableWorkflow.CorrelationId,
            BookmarkId = collectedResumableWorkflow.BookmarkId,
            Input = options?.Input,
            Properties = options?.Properties,
            CancellationToken = cancellationToken
        };

        return (await ResumeWorkflowAsync(match.WorkflowInstanceId, runtimeOptions))!;
    }

    /// <inheritdoc />
    public async Task<CancellationResult> CancelWorkflowAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var workflowExecutionContext = await workflowExecutionContextStore.FindAsync(workflowInstanceId);

        if (workflowExecutionContext is null)
        {
            // The execution context is not running on this instance.
            // It might not be running on any instance, so check the db and update the record.
            // Use lock to prevent race conditions and other instances from updating the workflow context
            await using var cancelLock = await distributedLockProvider.TryAcquireLockAsync($"{workflowInstanceId}-cancel", cancellationToken: cancellationToken);
            if (cancelLock == null)
                return new CancellationResult(false, CancellationFailureReason.CancellationAlreadyInProgress);

            var workflowInstance = await workflowInstanceStore.FindAsync(workflowInstanceId, cancellationToken);
            if (workflowInstance is null)
                return new CancellationResult(false, CancellationFailureReason.NotFound);
            if (workflowInstance.Status == WorkflowStatus.Finished)
                return new CancellationResult(false, CancellationFailureReason.AlreadyFinished);

            var workflowState = await ExportWorkflowStateAsync(workflowInstanceId, cancellationToken);

            if (workflowState == null)
                throw new Exception("Workflow state not found");

            var workflow = await workflowDefinitionService.FindWorkflowAsync(workflowState.DefinitionId, VersionOptions.SpecificVersion(workflowState.DefinitionVersion), cancellationToken);

            if (workflow == null)
                throw new Exception("Workflow definition not found");

            workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(serviceProvider, workflow, workflowState, cancellationToken: cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
                await CancelWorkflowExecutionContextAsync();

            return new CancellationResult(true);
        }

        await using var mainCancelLock = await distributedLockProvider.AcquireLockAsync($"{workflowInstanceId}-cancel", TimeSpan.FromMinutes(1), cancellationToken: cancellationToken);
        await CancelWorkflowExecutionContextAsync();
        return new CancellationResult(true);

        async Task CancelWorkflowExecutionContextAsync()
        {
            var originalBookmarks = workflowExecutionContext.Bookmarks.ToList();

            workflowExecutionContext.Cancel();

            var newBookmarks = workflowExecutionContext.Bookmarks.ToList();
            var diff = Diff.For(originalBookmarks, newBookmarks);
            var bookmarkRequest = new UpdateBookmarksRequest(workflowExecutionContext.Id,
                diff,
                workflowExecutionContext.CorrelationId);
            await bookmarksPersister.PersistBookmarksAsync(bookmarkRequest);

            var instance = await workflowInstanceManager.SaveAsync(workflowExecutionContext, cancellationToken);
            await workflowInstanceStore.SaveAsync(instance, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowMatch>> FindWorkflowsAsync(WorkflowsFilter filter, CancellationToken cancellationToken = default)
    {
        var startableWorkflows = await FindStartableWorkflowsAsync(filter, cancellationToken);
        var resumableWorkflows = await FindResumableWorkflowsAsync(filter, cancellationToken);
        var results = startableWorkflows.Concat(resumableWorkflows).ToList();
        return results;
    }

    /// <inheritdoc />
    public async Task<WorkflowState?> ExportWorkflowStateAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var workflowInstance = await workflowInstanceStore.FindAsync(workflowInstanceId, cancellationToken);
        return workflowStateMapper.Map(workflowInstance);
    }

    /// <inheritdoc />
    public async Task ImportWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var workflowInstance = workflowStateMapper.Map(workflowState)!;
        await workflowInstanceManager.SaveAsync(workflowInstance, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateBookmarkAsync(StoredBookmark bookmark, CancellationToken cancellationToken = default)
    {
        await bookmarkStore.SaveAsync(bookmark, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountRunningWorkflowsAsync(CountRunningWorkflowsRequest request, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowInstanceFilter
        {
            DefinitionId = request.DefinitionId,
            Version = request.Version,
            CorrelationId = request.CorrelationId,
            WorkflowStatus = WorkflowStatus.Running
        };
        return await workflowInstanceStore.CountAsync(filter, cancellationToken);
    }

    private async Task<WorkflowExecutionResult> StartWorkflowAsync(IWorkflowHost workflowHost, StartWorkflowRuntimeParams? @params = default)
    {
        var workflowInstanceId = string.IsNullOrEmpty(@params?.InstanceId)
            ? identityGenerator.GenerateId()
            : @params?.InstanceId;
        var cancellationToken = @params?.CancellationToken ?? default;

        await using (await AcquireLockAsync(workflowInstanceId!, cancellationToken))
        {
            var input = @params?.Input;
            var correlationId = @params?.CorrelationId;
            var startWorkflowOptions = new StartWorkflowRequest
            {
                IsExistingInstance = @params?.IsExistingInstance == true,
                ParentWorkflowInstanceId = @params?.ParentWorkflowInstanceId,
                InstanceId = workflowInstanceId,
                CorrelationId = correlationId,
                Input = input,
                Properties = @params?.Properties,
                TriggerActivityId = @params?.TriggerActivityId
            };
            await workflowHost.ExecuteWorkflowAsync(startWorkflowOptions, cancellationToken);
            var workflowState = workflowHost.WorkflowState;

            return new WorkflowExecutionResult(
                workflowState.Id,
                workflowState.Status,
                workflowState.SubStatus,
                workflowState.Bookmarks,
                workflowState.Incidents,
                default);
        }
    }

    private async Task<IWorkflowHost> CreateWorkflowHostAsync(string definitionId, StartWorkflowRuntimeParams? options, CancellationToken cancellationToken)
    {
        if (options?.IsExistingInstance == true)
        {
            var workflowState = await LoadWorkflowStateAsync(options.InstanceId!, cancellationToken);
            var workflow = await workflowDefinitionService.FindWorkflowAsync(workflowState.DefinitionVersionId, cancellationToken) ?? throw new Exception("Specified workflow definition and version does not exist");
            return await workflowHostFactory.CreateAsync(workflow, workflowState, cancellationToken);
        }
        else
        {
            var versionOptions = options?.VersionOptions;
            var workflow = await workflowDefinitionService.FindWorkflowAsync(definitionId, versionOptions ?? VersionOptions.Published, cancellationToken);

            if (workflow == null)
                throw new Exception("Specified workflow definition and version does not exist");

            var workflowInstanceId = string.IsNullOrEmpty(options?.InstanceId)
                ? identityGenerator.GenerateId()
                : options?.InstanceId;
            return await workflowHostFactory.CreateAsync(workflow, workflowInstanceId, cancellationToken);
        }
    }

    private async Task<WorkflowState> LoadWorkflowStateAsync(string instanceId, CancellationToken cancellationToken)
    {
        var workflowInstance = await workflowInstanceStore.FindAsync(instanceId, cancellationToken) ?? throw new Exception($"Workflow instance {instanceId} not found");
        return workflowStateMapper.Map(workflowInstance)!;
    }

    private async Task<ICollection<WorkflowExecutionResult>> ResumeWorkflowsAsync(IEnumerable<StoredBookmark> bookmarks, ResumeWorkflowRuntimeParams runtimeParams)
    {
        var resumedWorkflows = new List<WorkflowExecutionResult>();

        foreach (var bookmark in bookmarks)
        {
            var workflowInstanceId = bookmark.WorkflowInstanceId;

            var resumeResult = await ResumeWorkflowAsync(
                workflowInstanceId,
                new ResumeWorkflowRuntimeParams
                {
                    CorrelationId = runtimeParams.CorrelationId,
                    Input = runtimeParams.Input,
                    Properties = runtimeParams.Properties,
                    CancellationToken = runtimeParams.CancellationToken,
                    BookmarkId = bookmark.BookmarkId,
                    ActivityHandle = bookmark.ActivityInstanceId != null ? ActivityHandle.FromActivityInstanceId(bookmark.ActivityInstanceId) : null
                });

            if (resumeResult != null)
                resumedWorkflows.Add(new WorkflowExecutionResult(workflowInstanceId, resumeResult.Status,
                    resumeResult.SubStatus, resumeResult.Bookmarks, resumeResult.Incidents));
        }

        return resumedWorkflows;
    }

    private async Task SaveWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken)
    {
        await workflowInstanceManager.SaveAsync(workflowState, cancellationToken);
    }

    private async Task<IEnumerable<WorkflowMatch>> FindStartableWorkflowsAsync(WorkflowsFilter workflowsFilter, CancellationToken cancellationToken = default)
    {
        var results = new List<WorkflowMatch>();
        var hash = hasher.Hash(workflowsFilter.ActivityTypeName, workflowsFilter.BookmarkPayload);

        // Start new workflows. Notice that this happens in a process-synchronized fashion to avoid multiple instances from being created. 
        var sharedResource = $"{nameof(DefaultWorkflowRuntime)}__StartTriggeredWorkflows__{hash}";
        await using (await AcquireLockAsync(sharedResource, cancellationToken))
        {
            var filter = new TriggerFilter
            {
                Hash = hash
            };
            var triggers = await triggerStore.FindManyAsync(filter, cancellationToken);

            foreach (var trigger in triggers)
            {
                var definitionId = trigger.WorkflowDefinitionId;
                var startOptions = new StartWorkflowRuntimeParams
                {
                    CorrelationId = workflowsFilter.Options.CorrelationId,
                    Input = workflowsFilter.Options.Input,
                    Properties = workflowsFilter.Options.Properties,
                    VersionOptions = VersionOptions.Published,
                    TriggerActivityId = trigger.ActivityId,
                    InstanceId = workflowsFilter.Options.WorkflowInstanceId
                };
                var canStartResult = await CanStartWorkflowAsync(definitionId, startOptions);
                var workflow = await workflowDefinitionService.FindWorkflowAsync(definitionId, startOptions.VersionOptions, cancellationToken);

                if (workflow == null)
                    throw new Exception($"The workflow definition {definitionId} was not found");

                var createWorkflowInstanceRequest = new CreateWorkflowInstanceRequest
                {
                    Workflow = workflow,
                    CorrelationId = workflowsFilter.Options.CorrelationId,
                    Properties = workflowsFilter.Options.Properties,
                    Input = workflowsFilter.Options.Input,
                    WorkflowInstanceId = workflowsFilter.Options.WorkflowInstanceId
                };
                var workflowInstance = workflowInstanceFactory.CreateWorkflowInstance(createWorkflowInstanceRequest);

                if (canStartResult.CanStart)
                {
                    results.Add(new StartableWorkflowMatch(
                        workflowInstance.Id,
                        workflowInstance,
                        workflowsFilter.Options.CorrelationId,
                        trigger.ActivityId,
                        definitionId,
                        trigger.Payload));
                }
            }
        }

        return results;
    }

    private async Task<IEnumerable<WorkflowMatch>> FindResumableWorkflowsAsync(WorkflowsFilter workflowsFilter, CancellationToken cancellationToken = default)
    {
        var hash = hasher.Hash(workflowsFilter.ActivityTypeName, workflowsFilter.BookmarkPayload);
        var correlationId = workflowsFilter.Options.CorrelationId;
        var workflowInstanceId = workflowsFilter.Options.WorkflowInstanceId;
        var filter = new BookmarkFilter
        {
            Hash = hash,
            CorrelationId = correlationId,
            WorkflowInstanceId = workflowInstanceId
        };
        var bookmarks = await bookmarkStore.FindManyAsync(filter, cancellationToken);
        var collectedWorkflows = bookmarks.Select(b =>
            new ResumableWorkflowMatch(b.WorkflowInstanceId, default, correlationId, b.BookmarkId, b.Payload)).ToList();
        return collectedWorkflows;
    }

    private async Task<IDistributedSynchronizationHandle> AcquireLockAsync(string resource, CancellationToken cancellationToken)
    {
        return await distributedLockProvider.AcquireLockAsync(resource, TimeSpan.FromMinutes(2), cancellationToken);
    }
}