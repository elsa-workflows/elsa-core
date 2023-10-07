using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Middleware.Activities;

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
public class DefaultActivityInvokerMiddleware : IActivityExecutionMiddleware
{
    private readonly ActivityMiddlewareDelegate _next;

    /// <summary>
    /// Constructor
    /// </summary>
    public DefaultActivityInvokerMiddleware(ActivityMiddlewareDelegate next)
    {
        _next = next;
    }

    /// <inheritdoc />
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        
        var workflowExecutionContext = context.WorkflowExecutionContext;

        // Evaluate input properties.
        await EvaluateInputPropertiesAsync(context);

        // Check if the activity can be executed.
        if (!await context.Activity.CanExecuteAsync(context))
        {
            context.Status = ActivityStatus.Pending;
            context.AddExecutionLogEntry("Precondition Failed", "Cannot execute at this time");
            return;
        }

        context.Status = ActivityStatus.Running;

        // Execute activity.
        await ExecuteActivityAsync(context);

        // Reset execute delegate.
        workflowExecutionContext.ExecuteDelegate = null;

        // If a bookmark was used to resume, burn it if not burnt already by the activity.
        var resumedBookmark = workflowExecutionContext.ResumedBookmarkContext?.Bookmark;

        if (resumedBookmark is { AutoBurn: true })
            workflowExecutionContext.Bookmarks.Remove(resumedBookmark);

        // Update execution count.
        context.IncrementExecutionCount();

        // Invoke next middleware.
        await _next(context);

        // If the activity created any bookmarks, copy them into the workflow execution context.
        if (context.Bookmarks.Any())
        {
            // Store bookmarks.
            workflowExecutionContext.Bookmarks.AddRange(context.Bookmarks);
        }
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
}