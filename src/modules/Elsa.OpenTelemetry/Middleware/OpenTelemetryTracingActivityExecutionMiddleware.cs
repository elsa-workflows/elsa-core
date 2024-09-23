using System.Diagnostics;
using Elsa.Common.Contracts;
using Elsa.OpenTelemetry.Helpers;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Activity = System.Diagnostics.Activity;
using ActivityKind = System.Diagnostics.ActivityKind;

namespace Elsa.OpenTelemetry.Middleware;

/// <inheritdoc />
[UsedImplicitly]
public class OpenTelemetryTracingActivityExecutionMiddleware(ActivityMiddlewareDelegate next, ISystemClock systemClock) : IActivityExecutionMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        var activity = context.Activity;
        using var span = ElsaOpenTelemetry.ActivitySource.StartActivity($"ActivityExecution", ActivityKind.Internal, Activity.Current?.Context ?? default);

        if (span == null)
        {
            await next(context);
            return;
        }

        span.SetTag("activity.nodeId", activity.NodeId);
        span.SetTag("activity.type", activity.Type);
        span.SetTag("activity.name", activity.Name);
        span.SetTag("activityInstance.id", context.Id);
        
        span.AddEvent(new ActivityEvent("Executing", tags: new ActivityTagsCollection(new Dictionary<string, object?>
        {
            ["activityInstance.status"] = context.Status.ToString(),
        })));
        
        await next(context);

        if (context.Status == ActivityStatus.Faulted)
        {
            span.AddEvent(new ActivityEvent("Faulted"));
            span.SetStatus(ActivityStatusCode.Error);
            span.SetTag("error", true);
            span.SetTag("hasIncidents", true);

            var errorMessage = string.IsNullOrWhiteSpace(context.Exception?.Message) ? "Unknown error" : context.Exception.Message;
            span.SetTag("error.message", errorMessage);
            
            if (!string.IsNullOrEmpty(context.Exception?.StackTrace))
                span.SetTag("error.stackTrace", context.Exception.StackTrace);
        }
        else
            span.AddEvent(new ActivityEvent("Executed", tags: new ActivityTagsCollection(new Dictionary<string, object?>
            {
                ["activityInstance.status"] = context.Status.ToString(),
            })));
        
        span.SetTag("activityExecution.durationMs", (systemClock.UtcNow - span.StartTimeUtc).TotalMilliseconds);
    }
}

/// <summary>
/// Contains extension methods for <see cref="OpenTelemetryTracingActivityExecutionMiddleware"/>.
/// </summary>
[UsedImplicitly]
public static class OpenTelemetryTracingActivityExecutionMiddlewareExtensions
{
    /// Installs the <see cref="OpenTelemetryTracingActivityExecutionMiddleware"/> component in the workflow execution pipeline.
    public static IActivityExecutionPipelineBuilder UseActivityExecutionTracing(this IActivityExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.Insert<OpenTelemetryTracingActivityExecutionMiddleware>(0);
}