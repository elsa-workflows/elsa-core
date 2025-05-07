using System.Diagnostics;
using Elsa.Common;
using Elsa.Common.Contracts;
using Elsa.Extensions;
using Elsa.OpenTelemetry.Contracts;
using Elsa.OpenTelemetry.Helpers;
using Elsa.OpenTelemetry.Models;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Pipelines.ActivityExecution;
using JetBrains.Annotations;
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
        using var span = ElsaOpenTelemetry.ActivitySource.StartActivity($"execute activity {activity.Type}", ActivityKind.Internal, Activity.Current?.Context ?? default);

        if (span == null)
        {
            await next(context);
            return;
        }

        span.SetTag("operation.name", "elsa.activity.execution");
        span.SetTag("activity.id", activity.NodeId);
        span.SetTag("activity.node.id", activity.NodeId);
        span.SetTag("activity.type", activity.Type);
        span.SetTag("activity.name", activity.Name);
        span.SetTag("activity.version", activity.Version);
        span.SetTag("activity.instance.id", context.Id);
        
        var activityKind = context.ActivityDescriptor.Kind;
        if (activityKind == Elsa.Workflows.ActivityKind.Job || (activityKind == Workflows.ActivityKind.Task && activity.GetRunAsynchronously())) 
            span.SetTag("span.type", "job");

        span.AddEvent(new("executing"));

        await next(context);

        if (context.Status == ActivityStatus.Faulted)
        {
            span.AddEvent(new("faulted"));
            span.SetStatus(ActivityStatusCode.Error);

            var errorSpanHandlerContext = new ActivityErrorSpanContext(span, context.Exception);
            var errorSpanHandler = context.GetServices<IActivityErrorSpanHandler>()
                .OrderBy(x => x.Order)
                .FirstOrDefault(x => x.CanHandle(errorSpanHandlerContext));
            
            errorSpanHandler?.Handle(errorSpanHandlerContext);
        }
        else if (context.Status == ActivityStatus.Canceled)
        {
            span.AddEvent(new("canceled"));
        }
        else if (context.Status == ActivityStatus.Completed)
        {
            span.AddEvent(new("completed"));
        }
        else if (context.Status == ActivityStatus.Pending)
        {
            span.AddEvent(new("pending"));
        }
    }
}

/// <summary>
/// Contains extension methods for <see cref="OpenTelemetryTracingActivityExecutionMiddleware"/>.
/// </summary>
[UsedImplicitly]
public static class OpenTelemetryTracingActivityExecutionMiddlewareExtensions
{
    /// <summary>
    /// Installs the <see cref="OpenTelemetryTracingActivityExecutionMiddleware"/> component in the workflow execution pipeline.
    /// </summary>
    public static IActivityExecutionPipelineBuilder UseActivityExecutionTracing(this IActivityExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.Insert<OpenTelemetryTracingActivityExecutionMiddleware>(0);
}