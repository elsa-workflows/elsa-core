using Elsa.OpenTelemetry.Helpers;
using Elsa.Workflows;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using JetBrains.Annotations;
using Activity = System.Diagnostics.Activity;
using ActivityKind = System.Diagnostics.ActivityKind;

namespace Elsa.OpenTelemetry.Middleware;

[UsedImplicitly]
public class OpenTelemetryTracingWorkflowExecutionMiddleware(WorkflowMiddlewareDelegate next) : WorkflowExecutionMiddleware(next)
{
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var workflowInstanceId = context.Id;
        var workflow = context.Workflow;
        using var activity = ElsaOpenTelemetry.ActivitySource.StartActivity($"WorkflowExecution {workflow.WorkflowMetadata.Name}", ActivityKind.Internal, Activity.Current?.Context ?? default);
        activity?.AddTag("workflowInstance.id", workflowInstanceId);
        activity?.AddTag("workflowDefinition.definitionId", workflow.Identity.DefinitionId);
        activity?.AddTag("workflowDefinition.version", workflow.Identity.Version);
        activity?.AddTag("tenantId", workflow.Identity.TenantId);
        activity?.AddTag("workflowInstance.originalStatus", context.Status.ToString());
        activity?.AddTag("workflowInstance.originalSubStatus", context.SubStatus.ToString());
        await Next(context);
        activity?.AddTag("workflowInstance.newStatus", context.Status.ToString());
        activity?.AddTag("workflowInstance.newSubStatus", context.SubStatus.ToString());
    }
}

[UsedImplicitly]
public static class OpenTelemetryWorkflowExecutionMiddlewareExtensions
{
    /// Installs the <see cref="OpenTelemetryTracingWorkflowExecutionMiddleware"/> component in the workflow execution pipeline.
    public static IWorkflowExecutionPipelineBuilder UseWorkflowExecutionTracing(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<OpenTelemetryTracingWorkflowExecutionMiddleware>();
}
