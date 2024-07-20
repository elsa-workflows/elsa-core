using Elsa.OpenTelemetry.Helpers;
using Elsa.Workflows;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using JetBrains.Annotations;

namespace Elsa.OpenTelemetry.Middleware;

[UsedImplicitly]
public class OpenTelemetryTracingWorkflowExecutionMiddleware(WorkflowMiddlewareDelegate next) : WorkflowExecutionMiddleware(next)
{
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var workflowInstanceId = context.Id;
        using var activity = ElsaOpenTelemetry.ActivitySource.StartActivity("WorkflowExecution");
        activity?.AddTag("workflow.instanceId", workflowInstanceId);
        await Next(context);
        activity?.AddTag("workflow.status", context.Status.ToString());
        activity?.AddTag("workflow.subStatus", context.SubStatus.ToString());
    }
}

public static class OpenTelemetryWorkflowExecutionMiddlewareExtensions
{
    /// <summary>
    /// Installs the <see cref="OpenTelemetryTracingWorkflowExecutionMiddleware"/> component in the workflow execution pipeline.
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UseExceptionHandling(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<OpenTelemetryTracingWorkflowExecutionMiddleware>();
}
