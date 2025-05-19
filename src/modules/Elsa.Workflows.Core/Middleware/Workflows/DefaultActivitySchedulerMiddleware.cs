using Elsa.Extensions;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Middleware.Workflows;

/// <summary>
/// Installs middleware that executes scheduled work items.
/// </summary>
public static class UseActivitySchedulerMiddlewareExtensions
{
    /// <summary>
    /// Installs middleware that executes scheduled work items. 
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UseDefaultActivityScheduler(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<DefaultActivitySchedulerMiddleware>();
}

/// <summary>
/// A workflow execution middleware component that executes scheduled work items.
/// </summary>
public class DefaultActivitySchedulerMiddleware(WorkflowMiddlewareDelegate next, IActivityInvoker activityInvoker, ICommitStrategyRegistry commitStrategyRegistry, ILogger<DefaultActivitySchedulerMiddleware> logger) : WorkflowExecutionMiddleware(next)
{
    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var scheduler = context.Scheduler;

        logger.LogDebug("Transitioning workflow context to Executing state.");
        context.TransitionTo(WorkflowSubStatus.Executing);
        await ConditionallyCommitStateAsync(context, WorkflowLifetimeEvent.WorkflowExecuting);
        
        while (scheduler.HasAny)
        {
            // Do not start a workflow if cancellation has been requested.
            if (context.CancellationToken.IsCancellationRequested)
            {
                logger.LogDebug("Cancellation requested. Transitioning workflow context to Cancelled state.");
                break;
            }
            
            var currentWorkItem = scheduler.Take();
            await ExecuteWorkItemAsync(context, currentWorkItem);
        }
        
        await Next(context);
        
        if (context.Status == WorkflowStatus.Running)
        {
            var allActivitiesCompleted = context.AllActivitiesCompleted();
            logger.LogDebug("All activities completed: {AllActivitiesCompleted}.", allActivitiesCompleted);
            if (!allActivitiesCompleted)
            {
                var incompleteActivities = context.ActivityExecutionContexts.Where(x => !x.IsCompleted).ToList();
                logger.LogDebug("Incomplete activities: {IncompleteActivitiesCount}.", incompleteActivities.Count);
                foreach (var activityExecutionContext in incompleteActivities)
                {
                    var activity = activityExecutionContext.Activity;
                    var activityId = activity.Id;
                    var activityType = activity.Type;
                    var activityStatus = activityExecutionContext.Status;
                    logger.LogDebug("Activity {ActivityType}: {ActivityId} is in status {ActivityStatus}.", activityType, activityId, activityStatus);
                }
            }
            context.TransitionTo(allActivitiesCompleted ? WorkflowSubStatus.Finished : WorkflowSubStatus.Suspended);
        }
    }

    private async Task ExecuteWorkItemAsync(WorkflowExecutionContext context, ActivityWorkItem workItem)
    {
        var options = new ActivityInvocationOptions
        {
            Owner = workItem.Owner,
            ExistingActivityExecutionContext = workItem.ExistingActivityExecutionContext,
            Tag = workItem.Tag,
            Variables = workItem.Variables,
            Input = workItem.Input
        };

        logger.LogDebug("Executing scheduled activity {ActivityType}: {ActivityId}.", workItem.Activity.Type, workItem.Activity.Id);;
        await activityInvoker.InvokeAsync(context, workItem.Activity, options);
    }
    
    private async Task ConditionallyCommitStateAsync(WorkflowExecutionContext context, WorkflowLifetimeEvent lifetimeEvent)
    {
        var strategyName = context.Workflow.Options.CommitStrategyName;
        var strategy = string.IsNullOrWhiteSpace(strategyName) ? null : commitStrategyRegistry.FindWorkflowStrategy(strategyName);
        
        if(strategy == null)
            return;
        
        var strategyContext = new WorkflowCommitStateStrategyContext(context, lifetimeEvent);
        var commitAction = strategy.ShouldCommit(strategyContext);
        
        if (commitAction is CommitAction.Commit)
        {
            logger.LogDebug("Committing workflow context.");
            await context.CommitAsync();
        }
    }
}