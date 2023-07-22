using Elsa.Common.Contracts;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Abstractions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Notifications;
using Elsa.Workflows.Core.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class WorkflowRunner : IWorkflowRunner
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IWorkflowExecutionPipeline _pipeline;
    private readonly IWorkflowExecutionContextMapper _workflowExecutionContextMapper;
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly IWorkflowExecutionContextFactory _workflowExecutionContextFactory;
    private readonly ISystemClock _systemClock;
    private readonly INotificationSender _notificationSender;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowRunner(
        IServiceScopeFactory serviceScopeFactory,
        IWorkflowExecutionPipeline pipeline,
        IWorkflowExecutionContextMapper workflowExecutionContextMapper,
        IWorkflowBuilderFactory workflowBuilderFactory,
        IIdentityGenerator identityGenerator,
        IWorkflowExecutionContextFactory workflowExecutionContextFactory,
        ISystemClock systemClock,
        INotificationSender notificationSender)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _pipeline = pipeline;
        _workflowExecutionContextMapper = workflowExecutionContextMapper;
        _workflowBuilderFactory = workflowBuilderFactory;
        _identityGenerator = identityGenerator;
        _workflowExecutionContextFactory = workflowExecutionContextFactory;
        _systemClock = systemClock;
        _notificationSender = notificationSender;
    }

    /// <inheritdoc />
    public async Task<RunWorkflowResult> RunAsync(IActivity activity, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var workflow = Workflow.FromActivity(activity);
        return await RunAsync(workflow, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowResult> RunAsync(IWorkflow workflow, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var builder = _workflowBuilderFactory.CreateBuilder();
        var workflowDefinition = await builder.BuildWorkflowAsync(workflow, cancellationToken);
        return await RunAsync(workflowDefinition, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowResult<TResult>> RunAsync<TResult>(WorkflowBase<TResult> workflow, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync((IWorkflow)workflow, options, cancellationToken);
        return new RunWorkflowResult<TResult>(result.WorkflowState, result.Workflow, (TResult)result.Result!);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowResult> RunAsync<T>(RunWorkflowOptions? options = default, CancellationToken cancellationToken = default) where T : IWorkflow
    {
        var builder = _workflowBuilderFactory.CreateBuilder();
        var workflowDefinition = await builder.BuildWorkflowAsync<T>(cancellationToken);
        return await RunAsync(workflowDefinition, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TResult> RunAsync<T, TResult>(RunWorkflowOptions? options = default, CancellationToken cancellationToken = default) where T : WorkflowBase<TResult>
    {
        var builder = _workflowBuilderFactory.CreateBuilder();
        var workflowDefinition = await builder.BuildWorkflowAsync<T>(cancellationToken);
        var result = await RunAsync(workflowDefinition, options, cancellationToken);
        return (TResult)result.Result!;
    }

    /// <inheritdoc />
    public async Task<RunWorkflowResult> RunAsync(Workflow workflow, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        // Create a child scope.
        using var scope = _serviceScopeFactory.CreateScope();

        // Setup a workflow execution context.
        var instanceId = options?.WorkflowInstanceId ?? _identityGenerator.GenerateId();
        var input = options?.Input;
        var correlationId = options?.CorrelationId;
        var triggerActivityId = options?.TriggerActivityId;
        var workflowExecutionContext = await CreateWorkflowExecutionContextAsync(scope.ServiceProvider, workflow, instanceId, correlationId, default, input, default, triggerActivityId, cancellationToken);

        // Schedule the first activity.
        workflowExecutionContext.ScheduleWorkflow();

        return await RunAsync(workflowExecutionContext);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        // Create a child scope.
        using var scope = _serviceScopeFactory.CreateScope();

        // Create workflow execution context.
        var input = options?.Input;
        var correlationId = options?.CorrelationId ?? workflowState.CorrelationId;
        var triggerActivityId = options?.TriggerActivityId;
        var workflowExecutionContext = await CreateWorkflowExecutionContextAsync(scope.ServiceProvider, workflow, workflowState.Id, correlationId, workflowState, input, default, triggerActivityId, cancellationToken);
        var bookmarkId = options?.BookmarkId;
        var activityNodeId = options?.ActivityNodeId;
        var activityId = options?.ActivityId;
        var activityInstanceId = options?.ActivityInstanceId;
        var activityHash = options?.ActivityHash;

        if (bookmarkId != null)
        {
            // Schedule the bookmark.
            var bookmark = workflowState.Bookmarks.FirstOrDefault(x => x.Id == bookmarkId);

            if (bookmark != null)
                workflowExecutionContext.ScheduleBookmark(bookmark);
        }
        else if (activityNodeId != null)
        {
            // Schedule the activity.
            var activity = workflowExecutionContext.FindActivityByNodeId(activityNodeId);
            workflowExecutionContext.ScheduleActivity(activity);
        }
        else if (activityHash != null)
        {
            // Schedule the activity.
            var activity = workflowExecutionContext.FindActivityByHash(activityHash);
            workflowExecutionContext.ScheduleActivity(activity);
        }
        else if (activityId != null)
        {
            // Schedule the activity.
            var activity = workflowExecutionContext.FindActivityByActivityId(activityId);
            workflowExecutionContext.ScheduleActivity(activity);
        }
        else if (activityInstanceId != null)
        {
            // Schedule the activity.
            var activityExecutionContext = workflowExecutionContext.ActiveActivityExecutionContexts.FirstOrDefault(x => x.Id == activityInstanceId) ?? throw new Exception("No activity execution context found with the specified ID.");
            workflowExecutionContext.ScheduleActivityExecutionContext(activityExecutionContext);
        }
        else
        {
            // Schedule the workflow itself.
            //workflowExecutionContext.ScheduleRoot();
            workflowExecutionContext.ScheduleWorkflow();
        }

        return await RunAsync(workflowExecutionContext);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowResult> RunAsync(WorkflowExecutionContext workflowExecutionContext)
    {
        var workflow = workflowExecutionContext.Workflow;
        var cancellationToken = workflowExecutionContext.CancellationToken;

        // Publish domain event.
        await _notificationSender.SendAsync(new WorkflowExecuting(workflow, workflowExecutionContext), cancellationToken);

        // Transition into the Running state.
        workflowExecutionContext.TransitionTo(WorkflowSubStatus.Executing);

        // Execute the workflow execution pipeline.
        await _pipeline.ExecuteAsync(workflowExecutionContext);

        // Extract workflow state.
        var workflowState = workflowExecutionContext.TransientProperties.TryGetValue(workflowExecutionContext, out var state)
            ? (WorkflowState)state
            : _workflowExecutionContextMapper.Extract(workflowExecutionContext);
        
        // Update timestamps.
        workflowState.UpdatedAt = _systemClock.UtcNow;

        if (workflowState.Status == WorkflowStatus.Finished) 
            workflowState.FinishedAt = workflowState.UpdatedAt;

        // Read captured output, if any.
        var result = workflow.ResultVariable?.Get(workflowExecutionContext.MemoryRegister);

        // Publish domain event.
        await _notificationSender.SendAsync(new WorkflowExecuted(workflow, workflowState, workflowExecutionContext), cancellationToken);

        // Return workflow execution result containing state + bookmarks.
        return new RunWorkflowResult(workflowState, workflowExecutionContext.Workflow, result);
    }

    private async Task<WorkflowExecutionContext> CreateWorkflowExecutionContextAsync(
        IServiceProvider serviceProvider,
        Workflow workflow,
        string instanceId,
        string? correlationId,
        WorkflowState? workflowState,
        IDictionary<string, object>? input,
        ExecuteActivityDelegate? executeActivityDelegate,
        string? triggerActivityId,
        CancellationToken cancellationToken) =>
        await _workflowExecutionContextFactory.CreateAsync(serviceProvider, workflow, instanceId, workflowState, input, correlationId, executeActivityDelegate, triggerActivityId, cancellationToken);
}