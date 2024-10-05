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
        span.SetTag("activityExecution.startTimeUtc", span.StartTimeUtc);
        
        span.AddEvent(new ActivityEvent("Executing", tags: CreateStatusTags(context)));
        
        await next(context);

        if (context.Status == ActivityStatus.Faulted)
        {
            span.AddEvent(new ActivityEvent("Faulted", tags: CreateStatusTags(context)));
            span.SetStatus(ActivityStatusCode.Error);
            span.SetTag("error", true);
            span.SetTag("activityInstance.hasIncidents", true);

            var errorMessage = string.IsNullOrWhiteSpace(context.Exception?.Message) ? "Unknown error" : context.Exception.Message;
            span.SetTag("error.message", errorMessage);
            
            if (!string.IsNullOrEmpty(context.Exception?.StackTrace))
                span.SetTag("error.stackTrace", context.Exception.StackTrace);
        }
        else
        {
            span.AddEvent(new ActivityEvent("Executed", tags: CreateStatusTags(context)));
            span.SetStatus(ActivityStatusCode.Ok);
        }
        
        var now = systemClock.UtcNow;
        span.SetTag("activityExecution.endTimeUtc", now);
        span.SetTag("activityExecution.durationMs", (now - span.StartTimeUtc).TotalMilliseconds);
    }
    
    private ActivityTagsCollection CreateStatusTags(ActivityExecutionContext context)
    {
        return new ActivityTagsCollection(new Dictionary<string, object?>
        {
            ["activityInstance.status"] = context.Status.ToString()
        });
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