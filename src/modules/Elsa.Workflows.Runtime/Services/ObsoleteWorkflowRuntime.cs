using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Matches;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Runtime.Params;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Implements the now deprecated workflow runtime API methods. 
/// </summary>
public class ObsoleteWorkflowRuntime(
    Func<string?, CancellationToken, ValueTask<IWorkflowClient>> createClientAsync,
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowActivationStrategyEvaluator workflowActivationStrategyEvaluator,
    IStimulusSender stimulusSender,
    IStimulusHasher stimulusHasher,
    IBookmarkStore bookmarkStore,
    IWorkflowInstanceStore workflowInstanceStore,
    ITriggerBoundWorkflowService triggerBoundWorkflowService,
    IBookmarkBoundWorkflowService bookmarkBoundWorkflowService)
{
    public static ObsoleteWorkflowRuntime Create(IServiceProvider serviceProvider, Func<string?, CancellationToken, ValueTask<IWorkflowClient>> createClientAsync)
    {
        return ActivatorUtilities.CreateInstance<ObsoleteWorkflowRuntime>(serviceProvider, createClientAsync);
    }
    
    public async Task<CanStartWorkflowResult> CanStartWorkflowAsync(string definitionId, StartWorkflowRuntimeParams? options = null)
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

        return new(null, canStart);
    }

    public async Task<WorkflowExecutionResult> StartWorkflowAsync(string definitionId, StartWorkflowRuntimeParams? options = null)
    {
        var cancellationToken = options?.CancellationToken ?? CancellationToken.None;
        var client = await createClientAsync(options?.InstanceId, cancellationToken);
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

    public async Task<ICollection<WorkflowExecutionResult>> StartWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options = null)
    {
        var cancellationToken = options?.CancellationToken ?? CancellationToken.None;
        var metadata = new StimulusMetadata
        {
            CorrelationId = options?.CorrelationId,
            WorkflowInstanceId = options?.WorkflowInstanceId,
            Properties = options?.Properties,
            ActivityInstanceId = options?.ActivityInstanceId,
            Input = options?.Input
        };
        var result = await stimulusSender.SendAsync(activityTypeName, bookmarkPayload, metadata, cancellationToken);
        var results = result.WorkflowInstanceResponses.Select(x => new WorkflowExecutionResult(x.WorkflowInstanceId, x.Status, x.SubStatus, x.Bookmarks, x.Incidents)).ToList();
        return results;
    }

    public async Task<WorkflowExecutionResult?> TryStartWorkflowAsync(string definitionId, StartWorkflowRuntimeParams? options = null)
    {
        return await StartWorkflowAsync(definitionId, options);
    }

    public async Task<WorkflowExecutionResult?> ResumeWorkflowAsync(string workflowInstanceId, ResumeWorkflowRuntimeParams? options = null)
    {
        var cancellationToken = options?.CancellationToken ?? CancellationToken.None;
        var workflowClient = await createClientAsync(workflowInstanceId, cancellationToken);
        var exists = await workflowClient.InstanceExistsAsync(cancellationToken);

        if (!exists)
            return null;

        var runWorkflowRequest = new RunWorkflowInstanceRequest
        {
            Input = options?.Input,
            Properties = options?.Properties,
            ActivityHandle = options?.ActivityHandle,
            BookmarkId = options?.BookmarkId
        };

        var response = await workflowClient.RunInstanceAsync(runWorkflowRequest, cancellationToken);

        return new(response.WorkflowInstanceId, response.Status, response.SubStatus, response.Bookmarks, response.Incidents);
    }

    public async Task<ICollection<WorkflowExecutionResult>> ResumeWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options = null)
    {
        var cancellationToken = options?.CancellationToken ?? CancellationToken.None;
        var metadata = new StimulusMetadata
        {
            CorrelationId = options?.CorrelationId,
            WorkflowInstanceId = options?.WorkflowInstanceId,
            Properties = options?.Properties,
            ActivityInstanceId = options?.ActivityInstanceId,
            Input = options?.Input
        };
        var result = await stimulusSender.SendAsync(activityTypeName, bookmarkPayload, metadata, cancellationToken);
        var results = result.WorkflowInstanceResponses.Select(x => new WorkflowExecutionResult(x.WorkflowInstanceId, x.Status, x.SubStatus, x.Bookmarks, x.Incidents)).ToList();
        return results;
    }

    public async Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options = null)
    {
        var cancellationToken = options?.CancellationToken ?? CancellationToken.None;
        var metadata = new StimulusMetadata
        {
            CorrelationId = options?.CorrelationId,
            WorkflowInstanceId = options?.WorkflowInstanceId,
            Properties = options?.Properties,
            ActivityInstanceId = options?.ActivityInstanceId,
            Input = options?.Input
        };
        var result = await stimulusSender.SendAsync(activityTypeName, bookmarkPayload, metadata, cancellationToken);
        var results = result.WorkflowInstanceResponses.Select(x => new WorkflowExecutionResult(x.WorkflowInstanceId, x.Status, x.SubStatus, x.Bookmarks, x.Incidents)).ToList();
        return new(results);
    }

    public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(WorkflowMatch match, ExecuteWorkflowParams? options = default)
    {
        var cancellationToken = options?.CancellationToken ?? CancellationToken.None;
        if (match is StartableWorkflowMatch collectedStartableWorkflow)
        {
            var startOptions = new StartWorkflowRuntimeParams
            {
                CorrelationId = collectedStartableWorkflow.CorrelationId,
                Input = options?.Input,
                Properties = options?.Properties,
                VersionOptions = VersionOptions.Published,
                TriggerActivityId = collectedStartableWorkflow.ActivityId,
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
            CancellationToken = cancellationToken,
        };

        return (await ResumeWorkflowAsync(collectedResumableWorkflow.WorkflowInstanceId, runtimeOptions))!;
    }

    public async Task<CancellationResult> CancelWorkflowAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var client = await createClientAsync(workflowInstanceId, cancellationToken);
        await client.CancelAsync(cancellationToken);
        return new(true);
    }

    public async Task<IEnumerable<WorkflowMatch>> FindWorkflowsAsync(WorkflowsFilter filter, CancellationToken cancellationToken = default)
    {
        var startableWorkflows = await FindStartableWorkflowsAsync(filter, cancellationToken);
        var resumableWorkflows = await FindResumableWorkflowsAsync(filter, cancellationToken);
        var results = startableWorkflows.Concat(resumableWorkflows).ToList();
        return results;
    }

    public async Task<WorkflowState?> ExportWorkflowStateAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var client = await createClientAsync(workflowInstanceId, cancellationToken);
        return await client.ExportStateAsync(cancellationToken);
    }

    public async Task ImportWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var client = await createClientAsync(workflowState.Id, cancellationToken);
        await client.ImportStateAsync(workflowState, cancellationToken);
    }

    public async Task UpdateBookmarkAsync(StoredBookmark bookmark, CancellationToken cancellationToken = default)
    {
        await bookmarkStore.SaveAsync(bookmark, cancellationToken);
    }

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

    private async Task<IEnumerable<WorkflowMatch>> FindStartableWorkflowsAsync(WorkflowsFilter filter, CancellationToken cancellationToken = default)
    {
        var stimulusHash = stimulusHasher.Hash(filter.ActivityTypeName, filter.BookmarkPayload, filter.Options.ActivityInstanceId);
        var triggerBoundWorkflows = await triggerBoundWorkflowService.FindManyAsync(stimulusHash, cancellationToken).ToList();
        var correlationId = filter.Options.CorrelationId;

        var query =
                from triggerBoundWorkflow in triggerBoundWorkflows
                from trigger in triggerBoundWorkflow.Triggers
                select new StartableWorkflowMatch(correlationId, trigger.ActivityId, triggerBoundWorkflow.WorkflowGraph.Workflow.Identity.DefinitionId, filter.BookmarkPayload);

        return query.ToList();
    }

    private async Task<IEnumerable<WorkflowMatch>> FindResumableWorkflowsAsync(WorkflowsFilter filter, CancellationToken cancellationToken)
    {
        var bookmarkOptions = new FindBookmarkOptions
        {
            CorrelationId = filter.Options.CorrelationId,
            WorkflowInstanceId = filter.Options.WorkflowInstanceId,
            ActivityInstanceId = filter.Options.ActivityInstanceId
        };
        var bookmarkBoundWorkflows = await bookmarkBoundWorkflowService.FindManyAsync(filter.ActivityTypeName, filter.BookmarkPayload, bookmarkOptions, cancellationToken).ToList();

        return (
                from bookmarkBoundWorkflow in bookmarkBoundWorkflows
                from bookmark in bookmarkBoundWorkflow.Bookmarks
                select new ResumableWorkflowMatch(bookmarkBoundWorkflow.WorkflowInstanceId, bookmark.CorrelationId, bookmark.Id, bookmark.Payload))
            .ToList();
    }
}