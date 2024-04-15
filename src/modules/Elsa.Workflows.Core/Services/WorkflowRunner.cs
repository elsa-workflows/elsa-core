using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Options;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Services;

/// <inheritdoc />
public class WorkflowRunner : IWorkflowRunner
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkflowExecutionPipeline _pipeline;
    private readonly IWorkflowStateExtractor _workflowStateExtractor;
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly INotificationSender _notificationSender;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowRunner(
        IServiceProvider serviceProvider,
        IWorkflowExecutionPipeline pipeline,
        IWorkflowStateExtractor workflowStateExtractor,
        IWorkflowBuilderFactory workflowBuilderFactory,
        IIdentityGenerator identityGenerator,
        INotificationSender notificationSender)
    {
        _serviceProvider = serviceProvider;
        _pipeline = pipeline;
        _workflowStateExtractor = workflowStateExtractor;
        _workflowBuilderFactory = workflowBuilderFactory;
        _identityGenerator = identityGenerator;
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
    public async Task<RunWorkflowResult> RunAsync<T>(
        RunWorkflowOptions? options = default,
        CancellationToken cancellationToken = default) where T : IWorkflow, new()
    {
        var builder = _workflowBuilderFactory.CreateBuilder();
        var workflowDefinition = await builder.BuildWorkflowAsync<T>(cancellationToken);
        return await RunAsync(workflowDefinition, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TResult> RunAsync<T, TResult>(
        RunWorkflowOptions? options = default,
        CancellationToken cancellationToken = default) where T : WorkflowBase<TResult>, new()
    {
        var builder = _workflowBuilderFactory.CreateBuilder();
        var workflow = await builder.BuildWorkflowAsync<T>(cancellationToken);
        var result = await RunAsync(workflow, options, cancellationToken);
        return (TResult)result.Result!;
    }

    /// <inheritdoc />
    public async Task<RunWorkflowResult> RunAsync(Workflow workflow, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        // Set up a workflow execution context.
        var instanceId = options?.WorkflowInstanceId ?? _identityGenerator.GenerateId();
        var input = options?.Input;
        var properties = options?.Properties;
        var correlationId = options?.CorrelationId;
        var triggerActivityId = options?.TriggerActivityId;
        var parentWorkflowInstanceId = options?.ParentWorkflowInstanceId;
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(
            _serviceProvider,
            workflow,
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
    public async Task<RunWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        // Create workflow execution context.
        var input = options?.Input;
        var properties = options?.Properties;
        var correlationId = options?.CorrelationId ?? workflowState.CorrelationId;
        var triggerActivityId = options?.TriggerActivityId;
        var parentWorkflowInstanceId = options?.ParentWorkflowInstanceId;
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(
            _serviceProvider,
            workflow,
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
        
        if (bookmarkId != null)
        {
            var bookmark = workflowState.Bookmarks.FirstOrDefault(x => x.Id == bookmarkId);

            if (bookmark != null)
                workflowExecutionContext.ScheduleBookmark(bookmark);
        }
        else if (activityHandle != null)
        {
            if (activityHandle.ActivityInstanceId != null)
            {
                var activityExecutionContext = workflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Id == activityHandle.ActivityInstanceId) ?? throw new Exception("No activity execution context found with the specified ID.");
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
        var workflow = workflowExecutionContext.Workflow;
        var cancellationToken = workflowExecutionContext.CancellationToken;

        await _notificationSender.SendAsync(new WorkflowExecuting(workflow, workflowExecutionContext), cancellationToken);

        // If the status is Pending, it means the workflow is started for the first time.
        if (workflowExecutionContext.SubStatus == WorkflowSubStatus.Pending)
        {
            workflowExecutionContext.TransitionTo(WorkflowSubStatus.Executing);
            await _notificationSender.SendAsync(new WorkflowStarted(workflow, workflowExecutionContext), cancellationToken);
        }

        await _pipeline.ExecuteAsync(workflowExecutionContext);
        var workflowState = _workflowStateExtractor.Extract(workflowExecutionContext);

        if (workflowState.Status == WorkflowStatus.Finished)
        {
            await _notificationSender.SendAsync(new WorkflowFinished(workflow, workflowState, workflowExecutionContext), cancellationToken);
        }

        var result = workflow.ResultVariable?.Get(workflowExecutionContext.MemoryRegister);
        await _notificationSender.SendAsync(new WorkflowExecuted(workflow, workflowState, workflowExecutionContext), cancellationToken);
        return new RunWorkflowResult(workflowState, workflowExecutionContext.Workflow, result);
    }
}