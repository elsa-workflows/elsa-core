using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Middleware.Activities;

/// <summary>
/// Provides extension methods to <see cref="IActivityExecutionPipelineBuilder"/>.
/// </summary>
public static class ActivityInvokerMiddlewareExtensions
{
    /// <summary>
    /// Adds the <see cref="DefaultActivityInvokerMiddleware"/> component to the pipeline.
    /// </summary>
    public static IActivityExecutionPipelineBuilder UseDefaultActivityInvoker(this IActivityExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<DefaultActivityInvokerMiddleware>();
}

/// <summary>
/// A default activity execution middleware component that evaluates the current activity's properties, executes the activity and adds any produced bookmarks to the workflow execution context.
/// </summary>
public class DefaultActivityInvokerMiddleware(ActivityMiddlewareDelegate next, ICommitStrategyRegistry commitStrategyRegistry, ILogger<DefaultActivityInvokerMiddleware> logger)
    : IActivityExecutionMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var workflowExecutionContext = context.WorkflowExecutionContext;

        // Evaluate input properties.
        await EvaluateInputPropertiesAsync(context);

        // Prevent the activity from being started if cancellation is requested.
        if (context.CancellationToken.IsCancellationRequested)
        {
            context.TransitionTo(ActivityStatus.Canceled);
            context.AddExecutionLogEntry("Activity cancelled");
            return;
        }

        // Check if the activity can be executed.
        if (!await context.Activity.CanExecuteAsync(context))
        {
            context.TransitionTo(ActivityStatus.Pending);
            context.AddExecutionLogEntry("Precondition Failed", "Cannot execute at this time");
            return;
        }

        // Conditionally commit the workflow state.
        if (ShouldCommit(context, ActivityLifetimeEvent.ActivityExecuting))
            await context.WorkflowExecutionContext.CommitAsync();

        context.TransitionTo(ActivityStatus.Running);

        // Execute activity.
        await ExecuteActivityAsync(context);

        // Reset execute delegate.
        workflowExecutionContext.ExecuteDelegate = null;

        // If a bookmark was used to resume, burn it if not burnt already by the activity.
        var resumedBookmark = workflowExecutionContext.ResumedBookmarkContext?.Bookmark;

        if (resumedBookmark is { AutoBurn: true })
        {
            logger.LogDebug("Auto-burning bookmark {BookmarkId}", resumedBookmark.Id);
            workflowExecutionContext.Bookmarks.Remove(resumedBookmark);
        }

        // Update execution count.
        context.IncrementExecutionCount();

        // Invoke next middleware.
        await next(context);

        // If the activity created any bookmarks, copy them into the workflow execution context.
        if (context.Bookmarks.Any())
        {
            // Store bookmarks.
            workflowExecutionContext.Bookmarks.AddRange(context.Bookmarks);
            logger.LogDebug("Added {BookmarkCount} bookmarks to the workflow execution context", context.Bookmarks.Count);
        }

        // Conditionally commit the workflow state.
        if (ShouldCommit(context, ActivityLifetimeEvent.ActivityExecuted))
            await context.WorkflowExecutionContext.CommitAsync();
    }

    /// <summary>
    /// Executes the activity using the specified context.
    /// This method is virtual so that modules might override this implementation to do things like e.g. asynchronous processing.
    /// </summary>
    protected virtual async ValueTask ExecuteActivityAsync(ActivityExecutionContext context)
    {
        var executeDelegate = context.WorkflowExecutionContext.ExecuteDelegate;

        if (executeDelegate == null)
        {
            var methodInfo = typeof(IActivity).GetMethod(nameof(IActivity.ExecuteAsync))!;
            executeDelegate = (ExecuteActivityDelegate)Delegate.CreateDelegate(typeof(ExecuteActivityDelegate), context.Activity, methodInfo);
        }

        await executeDelegate(context);
    }

    private async Task EvaluateInputPropertiesAsync(ActivityExecutionContext context)
    {
        // Evaluate containing composite input properties, if any.
        var compositeContainerContexts = context.GetAncestors().Where(x => x.Activity is Composite).ToList();

        foreach (var activityExecutionContext in compositeContainerContexts)
        {
            if (!activityExecutionContext.GetHasEvaluatedProperties())
                await activityExecutionContext.EvaluateInputPropertiesAsync();
        }

        // Evaluate input properties.
        await context.EvaluateInputPropertiesAsync();
    }

    private bool ShouldCommit(ActivityExecutionContext context, ActivityLifetimeEvent lifetimeEvent)
    {
        var strategyName = context.Activity.GetCommitStrategy();
        var strategy = string.IsNullOrWhiteSpace(strategyName) ? null : commitStrategyRegistry.FindActivityStrategy(strategyName);
        var commitAction = CommitAction.Default;

        if (strategy != null)
        {
            var strategyContext = new ActivityCommitStateStrategyContext(context, lifetimeEvent);
            commitAction = strategy.ShouldCommit(strategyContext);
        }

        switch (commitAction)
        {
            case CommitAction.Skip:
                return false;
            case CommitAction.Commit:
                return true;
            case CommitAction.Default:
                {
                    var workflowStrategyName = context.WorkflowExecutionContext.Workflow.Options.CommitStrategyName;
                    var workflowStrategy = string.IsNullOrWhiteSpace(workflowStrategyName) ? null : commitStrategyRegistry.FindWorkflowStrategy(workflowStrategyName);
        
                    if(workflowStrategy == null)
                        return false;

                    var workflowLifetimeEvent = lifetimeEvent == ActivityLifetimeEvent.ActivityExecuting ? WorkflowLifetimeEvent.ActivityExecuting : WorkflowLifetimeEvent.ActivityExecuted;
                    var workflowCommitStateStrategyContext = new WorkflowCommitStateStrategyContext(context.WorkflowExecutionContext, workflowLifetimeEvent);
                    commitAction = workflowStrategy.ShouldCommit(workflowCommitStateStrategyContext);

                    return commitAction == CommitAction.Commit;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(commitAction), commitAction, "Unknown commit action");
        }
    }
}