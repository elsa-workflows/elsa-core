using System.Diagnostics;
using Elsa.OpenTelemetry.Helpers;
using Elsa.Workflows;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using JetBrains.Annotations;
using Activity = System.Diagnostics.Activity;
using ActivityKind = System.Diagnostics.ActivityKind;

namespace Elsa.OpenTelemetry.Middleware;

[UsedImplicitly]
public class OpenTelemetryTracingActivityExecutionMiddleware(ActivityMiddlewareDelegate next) : IActivityExecutionMiddleware
{
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        var activity = context.Activity;
        using var span = ElsaOpenTelemetry.ActivitySource.StartActivity($"ActivityExecution {context.ActivityDescriptor.TypeName}", ActivityKind.Internal, Activity.Current?.Context ?? default);
        span?.AddEvent(new ActivityEvent("Executing"));
        span?.AddTag("activity.nodeId", activity.NodeId);
        span?.AddTag("activityInstance.originalStatus", context.Status.ToString());
        await next(context);
        span?.AddTag("activityInstance.newStatus", context.Status.ToString());
        span?.AddEvent(new ActivityEvent("Executed"));
    }
}

[UsedImplicitly]
public static class OpenTelemetryTracingActivityExecutionMiddlewareExtensions
{
    /// Installs the <see cref="OpenTelemetryTracingActivityExecutionMiddleware"/> component in the workflow execution pipeline.
    public static IWorkflowExecutionPipelineBuilder UseWorkflowExecutionTracing(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<OpenTelemetryTracingWorkflowExecutionMiddleware>();
}
