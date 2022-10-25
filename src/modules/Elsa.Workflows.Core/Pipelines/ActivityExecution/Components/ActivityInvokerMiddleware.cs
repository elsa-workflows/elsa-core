using System.Linq;
using System.Threading.Tasks;
using Elsa.Common.Extensions;
using Elsa.Common.Services;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.Logging;
using Delegate = System.Delegate;

namespace Elsa.Workflows.Core.Pipelines.ActivityExecution.Components;

public static class ActivityInvokerMiddlewareExtensions
{
    public static IActivityExecutionBuilder UseDefaultActivityInvoker(this IActivityExecutionBuilder builder) => builder.UseMiddleware<ActivityInvokerMiddleware>();
}

public class ActivityInvokerMiddleware : IActivityExecutionMiddleware
{
    private readonly ActivityMiddlewareDelegate _next;
    private readonly ILogger _logger;

    public ActivityInvokerMiddleware(ActivityMiddlewareDelegate next, ILogger<ActivityInvokerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        var workflowExecutionContext = context.WorkflowExecutionContext;

        // Evaluate input properties.
        await context.EvaluateInputPropertiesAsync();
        var activity = context.Activity;

        // Execute activity.
        var methodInfo = typeof(IActivity).GetMethod(nameof(IActivity.ExecuteAsync))!;
        var executeDelegate = workflowExecutionContext.ExecuteDelegate ?? (ExecuteActivityDelegate)Delegate.CreateDelegate(typeof(ExecuteActivityDelegate), activity, methodInfo);
        
        await executeDelegate(context);

        // Reset execute delegate.
        workflowExecutionContext.ExecuteDelegate = null;

        // Invoke next middleware.
        await _next(context);

        // If the activity created any bookmarks, copy them into the workflow execution context.
        if (context.Bookmarks.Any())
        {
            // Store bookmarks.
            workflowExecutionContext.Bookmarks.AddRange(context.Bookmarks);
        }
    }
}