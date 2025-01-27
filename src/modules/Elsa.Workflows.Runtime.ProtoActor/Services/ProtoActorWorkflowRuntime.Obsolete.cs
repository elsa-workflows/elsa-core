using System.Diagnostics.CodeAnalysis;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Matches;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Runtime.Params;
using Elsa.Workflows.Runtime.ProtoActor.Extensions;
using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime.ProtoActor.Services;

public partial class ProtoActorWorkflowRuntime
{
        /// <inheritdoc />
    public async Task<CanStartWorkflowResult> CanStartWorkflowAsync(string definitionId, StartWorkflowRuntimeParams? options = default)
    {
        var cancellationToken = options?.CancellationToken ?? CancellationToken.None;
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(definitionId, options?.VersionOptions ?? VersionOptions.Published, cancellationToken);
        var workflow = workflowGraph!.Workflow;

        var canStart = await workflowActivationStrategyEvaluator.CanStartWorkflowAsync(new()
        {
            Workflow = workflow,
            CorrelationId = options?.CorrelationId,
            CancellationToken = cancellationToken
        });

        return new CanStartWorkflowResult(
        {
            CanStart = canStart,
            InstanceId = null
        };
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult?> TryStartWorkflowAsync(string definitionId, StartWorkflowRuntimeParams? options = default)
    {
        var cancellationToken = options?.CancellationToken ?? CancellationToken.None;
        var client = (LocalWorkflowClient)await CreateClientAsync(options?.InstanceId, cancellationToken);
        var createRequest = new CreateAndRunWorkflowInstanceRequest
        {
            Properties = options?.Properties,
            CorrelationId = options?.CorrelationId,
            Input = options?.Input,
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(definitionId, options?.VersionOptions ?? VersionOptions.Published),
            ParentId = options?.ParentWorkflowInstanceId,
            TriggerActivityId = options?.TriggerActivityId
        };
        var response = await client.CreateAndRunInstanceAsync(createRequest, cancellationToken);
        return new(response.WorkflowInstanceId, response.Status, response.SubStatus, response.Bookmarks, response.Incidents);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult> StartWorkflowAsync(string definitionId, StartWorkflowRuntimeParams? options = default)
    {
        var cancellationToken = options?.CancellationToken ?? CancellationToken.None;
        var client = (LocalWorkflowClient)await CreateClientAsync(options?.InstanceId, cancellationToken);
        var createRequest = new Workflows.Runtime.Messages.CreateAndRunWorkflowInstanceRequest
        {
            Properties = options?.Properties,
            CorrelationId = options?.CorrelationId,
            Input = options?.Input,
            WorkflowDefinitionHandle = Workflows.Models.WorkflowDefinitionHandle.ByDefinitionId(definitionId, options?.VersionOptions ?? VersionOptions.Published),
            ParentId = options?.ParentWorkflowInstanceId,
            TriggerActivityId = options?.TriggerActivityId
        };
        var response = await client.CreateAndRunInstanceAsync(createRequest, cancellationToken);
        return new(response.WorkflowInstanceId, response.Status, response.SubStatus, response.Bookmarks, response.Incidents);
    }

    /// <inheritdoc />
    public async Task<ICollection<WorkflowExecutionResult>> StartWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options = default)
    {
        var hash = _hasher.Hash(activityTypeName, bookmarkPayload);
        var filter = new TriggerFilter
        {
            Hash = hash
        };
        var systemCancellationToken = options?.CancellationTokens.SystemCancellationToken ?? default;
        var triggers = await _triggerStore.FindManyAsync(filter, systemCancellationToken);
        var results = new List<WorkflowExecutionResult>();

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
                CancellationTokens = options?.CancellationTokens ?? default
            };

            var canStartResult = await CanStartWorkflowAsync(definitionId, startOptions);

            // If we can't start the workflow, don't try it.
            if (!canStartResult.CanStart)
                continue;

            var startResult = await StartWorkflowAsync(definitionId, startOptions);
            results.Add(startResult);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult?> ResumeWorkflowAsync(string workflowInstanceId, ResumeWorkflowRuntimeParams? options = default)
    {
        var request = new ResumeWorkflowRequest
        {
            InstanceId = workflowInstanceId,
            CorrelationId = options?.CorrelationId.EmptyIfNull(),
            BookmarkId = options?.BookmarkId.EmptyIfNull(),
            ActivityId = options?.ActivityId.EmptyIfNull(),
            Input = options?.Input?.SerializeInput(),
            Properties = options?.Properties?.SerializeProperties(),
        };

        var client = _cluster.GetNamedWorkflowGrain(workflowInstanceId);
        var response = await client.Resume(request, options?.CancellationTokens.SystemCancellationToken ?? default);

        return _workflowExecutionResultMapper.Map(response!);
    }

    /// <inheritdoc />
    public async Task<ICollection<WorkflowExecutionResult>> ResumeWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options = default)
    {
        var hash = _hasher.Hash(activityTypeName, bookmarkPayload, options?.ActivityInstanceId);
        var correlationId = options?.CorrelationId;
        var workflowInstanceId = options?.WorkflowInstanceId;
        var filter = new BookmarkFilter
        {
            Hash = hash,
            CorrelationId = correlationId,
            WorkflowInstanceId = workflowInstanceId
        };
        var bookmarks = await _bookmarkStore.FindManyAsync(filter, options?.CancellationTokens.SystemCancellationToken ?? default);

        return await ResumeWorkflowsAsync(
            bookmarks,
            new ResumeWorkflowRuntimeParams
            {
                CorrelationId = correlationId,
                Input = options?.Input,
                Properties = options?.Properties,
                CancellationTokens = options?.CancellationTokens ?? default
            }
        );
    }

    /// <inheritdoc />
    public async Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options = default)
    {
        var startedWorkflows = await StartWorkflowsAsync(activityTypeName, bookmarkPayload, options);
        var resumedWorkflows = await ResumeWorkflowsAsync(activityTypeName, bookmarkPayload, options);
        var results = startedWorkflows.Concat(resumedWorkflows).ToList();

        return new TriggerWorkflowsResult(results);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(WorkflowMatch match, ExecuteWorkflowParams? options = default)
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
                CancellationTokens = options?.CancellationTokens ?? default
            };
            return await StartWorkflowAsync(collectedStartableWorkflow.DefinitionId!, startOptions);
        }

        var collectedResumableWorkflow = (match as ResumableWorkflowMatch)!;
        var runtimeOptions = new ResumeWorkflowRuntimeParams
        {
            CorrelationId = collectedResumableWorkflow.CorrelationId,
            Input = options?.Input,
            Properties = options?.Properties,
            BookmarkId = collectedResumableWorkflow.BookmarkId,
            CancellationTokens = options?.CancellationTokens ?? default
        };
        var result = await ResumeWorkflowAsync(match.WorkflowInstanceId, runtimeOptions);

        return result!;
    }

    /// <inheritdoc />
    public async Task<CancellationResult> CancelWorkflowAsync(string workflowInstanceId, CancellationToken cancellationToken)
    {
        var filter = new WorkflowInstanceFilter
        {
            Id = workflowInstanceId
        };

        var instance = await _workflowInstanceStore.FindAsync(filter, cancellationToken);
        if (instance is null)
            return new CancellationResult(false, FailureReason.NotFound);

        var client = _cluster.GetNamedWorkflowGrain(workflowInstanceId);
        var result = await client.Cancel(cancellationToken);
        return new CancellationResult(result?.Result ?? false);
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
    [RequiresUnreferencedCode("Calls Elsa.Workflows.Contracts.IWorkflowStateSerializer.DeserializeAsync(String, CancellationToken)")]
    public async Task<WorkflowState?> ExportWorkflowStateAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var client = _cluster.GetNamedWorkflowGrain(workflowInstanceId);
        var response = await client.ExportState(new ExportWorkflowStateRequest(), cancellationToken);
        var json = response!.SerializedWorkflowState.Text;
        var workflowState = await _workflowStateSerializer.DeserializeAsync(json, cancellationToken);
        return workflowState;
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Calls Elsa.Workflows.Contracts.IWorkflowStateSerializer.SerializeAsync(WorkflowState, CancellationToken)")]
    public async Task ImportWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var client = _cluster.GetNamedWorkflowGrain(workflowState.Id);
        var json = await _workflowStateSerializer.SerializeAsync(workflowState, cancellationToken);

        var request = new ImportWorkflowStateRequest
        {
            SerializedWorkflowState = new Json
            {
                Text = json
            }
        };

        await client.ImportState(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateBookmarkAsync(StoredBookmark bookmark, CancellationToken cancellationToken = default)
    {
        await _bookmarkStore.SaveAsync(bookmark, cancellationToken);
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
        return await _workflowInstanceStore.CountAsync(filter, cancellationToken);
    }

    private async Task<ICollection<WorkflowExecutionResult>> ResumeWorkflowsAsync(IEnumerable<StoredBookmark> bookmarks, ResumeWorkflowRuntimeParams runtimeParams)
    {
        var resumedWorkflows = new List<WorkflowExecutionResult>();

        foreach (var bookmark in bookmarks)
        {
            var workflowInstanceId = bookmark.WorkflowInstanceId;

            var newRuntimeOptions = new ResumeWorkflowRuntimeParams
            {
                CorrelationId = runtimeParams.CorrelationId,
                Input = runtimeParams.Input,
                Properties = runtimeParams.Properties,
                BookmarkId = bookmark.BookmarkId,
                ActivityId = runtimeParams.ActivityId,
                ActivityNodeId = runtimeParams.ActivityNodeId,
                ActivityInstanceId = runtimeParams.ActivityInstanceId,
                ActivityHash = runtimeParams.ActivityHash,
                CancellationTokens = runtimeParams.CancellationTokens
            };

            var resumeResult = await ResumeWorkflowAsync(workflowInstanceId, newRuntimeOptions);
            resumedWorkflows.Add(resumeResult!);
        }

        return resumedWorkflows;
    }

    private async Task<IEnumerable<WorkflowMatch>> FindStartableWorkflowsAsync(WorkflowsFilter workflowsFilter, CancellationToken cancellationToken)
    {
        var hash = _hasher.Hash(workflowsFilter.ActivityTypeName, workflowsFilter.BookmarkPayload);
        var filter = new TriggerFilter
        {
            Hash = hash
        };
        var triggers = await _triggerStore.FindManyAsync(filter, cancellationToken);
        var results = new List<WorkflowMatch>();

        foreach (var trigger in triggers)
        {
            var definitionId = trigger.WorkflowDefinitionId;

            var startOptions = new StartWorkflowRuntimeParams
            {
                CorrelationId = workflowsFilter.Options?.CorrelationId,
                Input = workflowsFilter.Options.Input,
                Properties = workflowsFilter.Options.Properties,
                VersionOptions = VersionOptions.Published,
                TriggerActivityId = trigger.ActivityId,
                CancellationTokens = cancellationToken
            };

            var canStartResult = await CanStartWorkflowAsync(definitionId, startOptions);
            var workflowGraph = await _workflowDefinitionService.FindWorkflowGraphAsync(trigger.WorkflowDefinitionVersionId, cancellationToken);

            if (workflowGraph == null)
            {
                _logger.LogWarning("Workflow version ID {DefinitionVersionId} not found", trigger.WorkflowDefinitionVersionId);
                continue;
            }

            var workflow = workflowGraph.Workflow;
            var createWorkflowInstanceRequest = new CreateWorkflowInstanceRequest
            {
                Workflow = workflow,
                CorrelationId = workflowsFilter.Options.CorrelationId,
                WorkflowInstanceId = workflowsFilter.Options?.WorkflowInstanceId,
                Input = workflowsFilter.Options?.Input,
                Properties = workflowsFilter.Options?.Properties
            };
            var workflowInstance = _workflowInstanceFactory.CreateWorkflowInstance(createWorkflowInstanceRequest);

            if (canStartResult.CanStart)
                results.Add(new StartableWorkflowMatch(workflowInstance.Id, workflowInstance, workflowsFilter.Options?.CorrelationId, trigger.ActivityId, definitionId, trigger.Payload));
        }

        return results;
    }

    private async Task<IEnumerable<WorkflowMatch>> FindResumableWorkflowsAsync(WorkflowsFilter workflowsFilter, CancellationToken cancellationToken)
    {
        var hash = _hasher.Hash(workflowsFilter.ActivityTypeName, workflowsFilter.BookmarkPayload);
        var correlationId = workflowsFilter.Options.CorrelationId;
        var workflowInstanceId = workflowsFilter.Options.WorkflowInstanceId;
        var activityInstanceId = workflowsFilter.Options.ActivityInstanceId;
        var filter = new BookmarkFilter
        {
            Hash = hash,
            CorrelationId = correlationId,
            WorkflowInstanceId = workflowInstanceId,
            ActivityInstanceId = activityInstanceId
        };
        var bookmarks = await _bookmarkStore.FindManyAsync(filter, cancellationToken);
        var collectedWorkflows = bookmarks.Select(b => new ResumableWorkflowMatch(b.WorkflowInstanceId, default, correlationId, b.BookmarkId, b.Payload)).ToList();
        return collectedWorkflows;
    }
}