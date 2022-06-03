using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.Logging;
using Delegate = System.Delegate;

namespace Elsa.Workflows.Core.Pipelines.ActivityExecution.Components;

public static class InvokeDriversMiddlewareExtensions
{
    public static IActivityExecutionBuilder UseDefaultActivityInvoker(this IActivityExecutionBuilder builder) => builder.UseMiddleware<ActivityInvokerMiddleware>();
}

public class ActivityInvokerMiddleware : IActivityExecutionMiddleware
{
    private readonly ActivityMiddlewareDelegate _next;
    private readonly ISystemClock _clock;
    private readonly ILogger _logger;

    public ActivityInvokerMiddleware(ActivityMiddlewareDelegate next, ISystemClock clock, ILogger<ActivityInvokerMiddleware> logger)
    {
        _next = next;
        _clock = clock;
        _logger = logger;
    }

    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        var workflowExecution = context.WorkflowExecutionContext;

        // Evaluate input properties.
        await context.EvaluateInputPropertiesAsync();
        var activity = context.Activity;

        // Execute activity.
        var methodInfo = typeof(IActivity).GetMethod(nameof(IActivity.ExecuteAsync))!;
        var executeDelegate = workflowExecution.ExecuteDelegate ?? (ExecuteActivityDelegate)Delegate.CreateDelegate(typeof(ExecuteActivityDelegate), activity, methodInfo);

        // Record executing event.
        _logger.LogTrace("Executing activity {ActivityId}", activity.Id);

        await executeDelegate(context);

        // Record executed event.
        _logger.LogTrace("Executed activity {ActivityId}", activity.Id);

        // Reset execute delegate.
        workflowExecution.ExecuteDelegate = null;

        // Invoke next middleware.
        await _next(context);

        // Exit if any bookmarks were created.
        if (context.Bookmarks.Any())
        {
            // Store bookmarks.
            workflowExecution.RegisterBookmarks(context.Bookmarks);
        }
    }
}