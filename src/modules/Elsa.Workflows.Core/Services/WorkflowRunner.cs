using Elsa.Common.Contracts;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Notifications;
using Elsa.Workflows.Core.Options;
using Elsa.Workflows.Core.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class WorkflowRunner : IWorkflowRunner
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IWorkflowExecutionPipeline _pipeline;
    private readonly IWorkflowStateExtractor _workflowStateExtractor;
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ISystemClock _systemClock;
    private readonly INotificationSender _notificationSender;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowRunner(
        IServiceScopeFactory serviceScopeFactory,
        IWorkflowExecutionPipeline pipeline,
        IWorkflowStateExtractor workflowStateExtractor,
        IWorkflowBuilderFactory workflowBuilderFactory,
        IIdentityGenerator identityGenerator,
        ISystemClock systemClock,
        INotificationSender notificationSender)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _pipeline = pipeline;
        _workflowStateExtractor = workflowStateExtractor;
        _workflowBuilderFactory = workflowBuilderFactory;
        _identityGenerator = identityGenerator;
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
        var workflow = await builder.BuildWorkflowAsync<T>(cancellationToken);
        var result = await RunAsync(workflow, options, cancellationToken);
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
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(scope.ServiceProvider, workflow, instanceId, correlationId, input, default, triggerActivityId, options?.CancellationTokens ?? cancellationToken);

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
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(scope.ServiceProvider, workflow, workflowState, correlationId, input, default, triggerActivityId, options?.CancellationTokens ?? cancellationToken);
        var bookmarkId = options?.BookmarkId;
        var activityNodeId = options?.ActivityNodeId;
        var activityId = options?.ActivityId;
        var activityInstanceId = options?.ActivityInstanceId;
        var activityHash = options?.ActivityHash;

        if (bookmarkId != null)
        {
            var bookmark = workflowState.Bookmarks.FirstOrDefault(x => x.Id == bookmarkId);

            if (bookmark != null)
                workflowExecutionContext.ScheduleBookmark(bookmark);
        }
        else if (activityNodeId != null)
        {
            var activity = workflowExecutionContext.FindActivityByNodeId(activityNodeId);
            if (activity != null) workflowExecutionContext.ScheduleActivity(activity);
        }
        else if (activityHash != null)
        {
            var activity = workflowExecutionContext.FindActivityByHash(activityHash);
            if (activity != null) workflowExecutionContext.ScheduleActivity(activity);
        }
        else if (activityId != null)
        {
            var activity = workflowExecutionContext.FindActivityById(activityId);
            if (activity != null) workflowExecutionContext.ScheduleActivity(activity);
        }
        else if (activityInstanceId != null)
        {
            var activityExecutionContext = workflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Id == activityInstanceId) ?? throw new Exception("No activity execution context found with the specified ID.");
            workflowExecutionContext.ScheduleActivityExecutionContext(activityExecutionContext);
        }
        else if (workflowExecutionContext.Scheduler.HasAny)
        {
            // Do nothing. The scheduler already has activities to schedule.
        }
        else
        {
            // Nothing was scheduled. Schedule the workflow itself.
            workflowExecutionContext.ScheduleWorkflow();
        }

        return await RunAsync(workflowExecutionContext);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowResult> RunAsync(WorkflowExecutionContext workflowExecutionContext)
    {
        var workflow = workflowExecutionContext.Workflow;
        var applicationCancellationToken = workflowExecutionContext.CancellationTokens.ApplicationCancellationToken;
        var systemCancellationToken = workflowExecutionContext.CancellationTokens.SystemCancellationToken;

        await _notificationSender.SendAsync(new WorkflowExecuting(workflow, workflowExecutionContext), applicationCancellationToken);

        // If the status is Pending, it means the workflow is started for the first time.
        if (workflowExecutionContext.SubStatus == WorkflowSubStatus.Pending)
        {
            workflowExecutionContext.TransitionTo(WorkflowSubStatus.Executing);
            await _notificationSender.SendAsync(new WorkflowStarted(workflow, workflowExecutionContext), applicationCancellationToken);
        }
        
        await _pipeline.ExecuteAsync(workflowExecutionContext);
        var workflowState = _workflowStateExtractor.Extract(workflowExecutionContext);
        workflowState.UpdatedAt = _systemClock.UtcNow;
        
        if (workflowState.Status == WorkflowStatus.Finished)
        {
            workflowState.FinishedAt = workflowState.UpdatedAt;
            await _notificationSender.SendAsync(new WorkflowFinished(workflow, workflowState, workflowExecutionContext), applicationCancellationToken);
        }
        
        var result = workflow.ResultVariable?.Get(workflowExecutionContext.MemoryRegister);
        await _notificationSender.SendAsync(new WorkflowExecuted(workflow, workflowState, workflowExecutionContext), systemCancellationToken);
        return new RunWorkflowResult(workflowState, workflowExecutionContext.Workflow, result);
    }
}