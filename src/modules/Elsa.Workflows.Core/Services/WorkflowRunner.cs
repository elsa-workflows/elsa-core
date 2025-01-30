using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Models;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Options;
using Elsa.Workflows.State;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows;

/// <inheritdoc />
public class WorkflowRunner(
    IServiceProvider serviceProvider,
    IWorkflowExecutionPipeline pipeline,
    IWorkflowStateExtractor workflowStateExtractor,
    IWorkflowBuilderFactory workflowBuilderFactory,
    IWorkflowGraphBuilder workflowGraphBuilder,
    IIdentityGenerator identityGenerator,
    INotificationSender notificationSender,
    ILoggerStateGenerator<WorkflowExecutionContext> loggerStateGenerator,
    ICommitStateHandler commitStateHandler,
    ILogger<WorkflowRunner> logger)
    : IWorkflowRunner
{
    /// <inheritdoc />
    public async Task<RunWorkflowResult> RunAsync(IActivity activity, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var workflow = Workflow.FromActivity(activity);
        var workflowGraph = await workflowGraphBuilder.BuildAsync(workflow, cancellationToken);
        return await RunAsync(workflowGraph, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowResult> RunAsync(IWorkflow workflow, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var builder = workflowBuilderFactory.CreateBuilder();
        var workflowDefinition = await builder.BuildWorkflowAsync(workflow, cancellationToken);
        return await RunAsync(workflowDefinition, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowResult<TResult>> RunAsync<TResult>(WorkflowBase<TResult> workflow, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync((IWorkflow)workflow, options, cancellationToken);
        return new(result.WorkflowState, result.Workflow, (TResult)result.Result!);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowResult> RunAsync<T>(RunWorkflowOptions? options = default, CancellationToken cancellationToken = default) where T : IWorkflow, new()
    {
        var builder = workflowBuilderFactory.CreateBuilder();
        var workflowDefinition = await builder.BuildWorkflowAsync<T>(cancellationToken);
        return await RunAsync(workflowDefinition, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TResult> RunAsync<T, TResult>(RunWorkflowOptions? options = default, CancellationToken cancellationToken = default) where T : WorkflowBase<TResult>, new()
    {
        var builder = workflowBuilderFactory.CreateBuilder();
        var workflow = await builder.BuildWorkflowAsync<T>(cancellationToken);
        var result = await RunAsync(workflow, options, cancellationToken);
        return (TResult)result.Result!;
    }

    /// <inheritdoc />
    public async Task<RunWorkflowResult> RunAsync(WorkflowGraph workflowGraph, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        // Set up a workflow execution context.
        var instanceId = options?.WorkflowInstanceId ?? identityGenerator.GenerateId();
        var input = options?.Input;
        var properties = options?.Properties;
        var correlationId = options?.CorrelationId;
        var triggerActivityId = options?.TriggerActivityId;
        var parentWorkflowInstanceId = options?.ParentWorkflowInstanceId;
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(
            serviceProvider,
            workflowGraph,
            instanceId,
            correlationId,
            parentWorkflowInstanceId,
            input,
            properties,
            default,
            triggerActivityId,
            cancellationToken);

        // Schedule the first activity.
        workflowExecutionContext.ScheduleWorkflow();

        return await RunAsync(workflowExecutionContext);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowResult> RunAsync(Workflow workflow, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var workflowGraph = await workflowGraphBuilder.BuildAsync(workflow, cancellationToken);
        return await RunAsync(workflowGraph, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var workflowGraph = await workflowGraphBuilder.BuildAsync(workflow, cancellationToken);
        return await RunAsync(workflowGraph, workflowState, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowResult> RunAsync(WorkflowGraph workflowGraph, WorkflowState workflowState, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        // Create a workflow execution context.
        var input = options?.Input;
        var properties = options?.Properties;
        var correlationId = options?.CorrelationId ?? workflowState.CorrelationId;
        var triggerActivityId = options?.TriggerActivityId;
        var parentWorkflowInstanceId = options?.ParentWorkflowInstanceId;
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(
            serviceProvider,
            workflowGraph,
            workflowState,
            correlationId,
            parentWorkflowInstanceId,
            input,
            properties,
            default,
            triggerActivityId,
            cancellationToken);

        var bookmarkId = options?.BookmarkId;
        var activityHandle = options?.ActivityHandle;

        if (!string.IsNullOrEmpty(bookmarkId))
        {
            var bookmark = workflowState.Bookmarks.FirstOrDefault(x => x.Id == bookmarkId);

            if (bookmark != null)
                workflowExecutionContext.ScheduleBookmark(bookmark);
        }
        else if (activityHandle != null)
        {
            if (!string.IsNullOrEmpty(activityHandle.ActivityInstanceId))
            {
                var activityExecutionContext = workflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Id == activityHandle.ActivityInstanceId)
                                               ?? throw new("No activity execution context found with the specified ID.");
                workflowExecutionContext.ScheduleActivityExecutionContext(activityExecutionContext);
            }
            else
            {
                var activity = workflowExecutionContext.FindActivity(activityHandle);
                if (activity != null) workflowExecutionContext.ScheduleActivity(activity);
            }
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
        var loggerState = loggerStateGenerator.GenerateLoggerState(workflowExecutionContext);
        using var loggingScope = logger.BeginScope(loggerState);
        var workflow = workflowExecutionContext.Workflow;
        var cancellationToken = workflowExecutionContext.CancellationToken;

        await notificationSender.SendAsync(new WorkflowExecuting(workflow, workflowExecutionContext), cancellationToken);

        // If the status is Pending, it means the workflow is started for the first time.
        if (workflowExecutionContext.SubStatus == WorkflowSubStatus.Pending)
        {
            workflowExecutionContext.TransitionTo(WorkflowSubStatus.Executing);
            await notificationSender.SendAsync(new WorkflowStarted(workflow, workflowExecutionContext), cancellationToken);
        }

        await pipeline.ExecuteAsync(workflowExecutionContext);
        var workflowState = workflowStateExtractor.Extract(workflowExecutionContext);

        if (workflowState.Status == WorkflowStatus.Finished)
        {
            await notificationSender.SendAsync(new WorkflowFinished(workflow, workflowState, workflowExecutionContext), cancellationToken);
        }

        var result = workflow.ResultVariable?.Get(workflowExecutionContext.MemoryRegister);
        await notificationSender.SendAsync(new WorkflowExecuted(workflow, workflowState, workflowExecutionContext), cancellationToken);
        await commitStateHandler.CommitAsync(workflowExecutionContext, workflowState, cancellationToken);
        return new(workflowState, workflowExecutionContext.Workflow, result);
    }
}