using System.Text.Json;
using Elsa.Contracts;
using Elsa.Models;
using Microsoft.Extensions.Logging;
using Delegate = System.Delegate;
using JsonNode = System.Text.Json.Nodes.JsonNode;
using JsonObject = System.Text.Json.Nodes.JsonObject;

namespace Elsa.Pipelines.ActivityExecution.Components;

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
        await EvaluateInputPropertiesAsync(context);
        var activity = context.Activity;

        // Execute activity.
        var methodInfo = typeof(IActivity).GetMethod(nameof(IActivity.ExecuteAsync))!;
        var executeDelegate = workflowExecution.ExecuteDelegate ?? (ExecuteActivityDelegate)Delegate.CreateDelegate(typeof(ExecuteActivityDelegate), activity, methodInfo);

        // Record executing event.
        LogExecutionRecord(context, WorkflowExecutionLogEventNames.Executing);

        await executeDelegate(context);

        // Record executed event.
        var payload = context.JournalData.Any() ? context.JournalData : default;
        LogExecutionRecord(context, WorkflowExecutionLogEventNames.Executed, payload: payload);

        // Reset execute delegate.
        workflowExecution.ExecuteDelegate = null;

        // Invoke next middleware.
        await _next(context);

        // Exit if any bookmarks were created.
        if (context.Bookmarks.Any())
        {
            // Store bookmarks.
            workflowExecution.RegisterBookmarks(context.Bookmarks);

            // Block current path of execution.
            return;
        }

        // Complete parent chain.
        await CompleteParentsAsync(context);
    }

    private void LogExecutionRecord(ActivityExecutionContext context, string eventName, string? message = default, string? source = default, object? payload = default) => context.AddExecutionLogEntry(eventName, message, source, payload);

    private async Task EvaluateInputPropertiesAsync(ActivityExecutionContext context)
    {
        var activity = context.Activity;
        var inputs = activity.GetInputs();
        var assignedInputs = inputs.Where(x => x.LocationReference != null!).ToList();
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var expressionExecutionContext = context.ExpressionExecutionContext;

        foreach (var input in assignedInputs)
        {
            var locationReference = input.LocationReference;
            var value = await evaluator.EvaluateAsync(input, expressionExecutionContext);
            locationReference.Set(context, value);
        }
    }

    private async Task CompleteParentsAsync(ActivityExecutionContext context)
    {
        var workflowExecutionContext = context.WorkflowExecutionContext;
        var currentContext = context;
        var currentParentContext = context.ParentActivityExecutionContext;

        while (currentParentContext != null)
        {
            var scheduledNodes = workflowExecutionContext.Scheduler.List().Select(x => x.ActivityId).ToList();
            var descendantNodes = currentParentContext.ActivityNode.Descendants().Select(x => x.Activity.Id).Distinct().ToList();
            var hasScheduledChildren = scheduledNodes.Intersect(descendantNodes).Any();
            var @continue = currentContext.Continue;

            // Do not continue if the activity instructed not to.
            if (!@continue)
                return;

            if (!hasScheduledChildren)
            {
                // Invoke completion callbacks.
                var completionCallback = workflowExecutionContext.PopCompletionCallback(currentParentContext, currentContext.Activity);

                if (completionCallback != null)
                    await completionCallback.Invoke(currentParentContext, currentContext);

                // Remove current activity context.
                workflowExecutionContext.ActivityExecutionContexts.Remove(currentContext);
            }

            // Do not continue completion callbacks of parents while there are scheduled nodes.
            if (workflowExecutionContext.Scheduler.HasAny)
                return;

            currentContext = currentParentContext;
            currentParentContext = currentContext.ParentActivityExecutionContext;
        }
    }
}