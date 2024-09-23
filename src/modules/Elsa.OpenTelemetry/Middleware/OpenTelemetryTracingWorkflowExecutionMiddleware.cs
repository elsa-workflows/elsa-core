using System.Diagnostics;
using Elsa.OpenTelemetry.Helpers;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using JetBrains.Annotations;
using Activity = System.Diagnostics.Activity;
using ActivityKind = System.Diagnostics.ActivityKind;

namespace Elsa.OpenTelemetry.Middleware;

/// <summary>
/// Middleware that traces workflow execution using OpenTelemetry.
/// </summary>
[UsedImplicitly]
public class OpenTelemetryTracingWorkflowExecutionMiddleware(WorkflowMiddlewareDelegate next) : WorkflowExecutionMiddleware(next)
{
    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var workflowInstanceId = context.Id;
        var workflow = context.Workflow;
        using var activity = ElsaOpenTelemetry.ActivitySource.StartActivity($"WorkflowExecution {workflow.WorkflowMetadata.Name}", ActivityKind.Internal, Activity.Current?.Context ?? default);
        
        if(!string.IsNullOrWhiteSpace(context.CorrelationId))
            activity?.AddTag("correlationId", context.CorrelationId);
        
        activity?.AddTag("workflowInstance.id", workflowInstanceId);
        activity?.AddTag("workflowDefinition.definitionId", workflow.Identity.DefinitionId);
        activity?.AddTag("workflowDefinition.version", workflow.Identity.Version);
        activity?.AddTag("workflowInstance.originalStatus", context.Status.ToString());
        activity?.AddTag("workflowInstance.originalSubStatus", context.SubStatus.ToString());
        activity?.AddEvent(new ActivityEvent("Executing"));
        await Next(context);
        activity?.AddEvent(new ActivityEvent("Executed"));
        activity?.AddTag("workflowInstance.newStatus", context.Status.ToString());
        activity?.AddTag("workflowInstance.newSubStatus", context.SubStatus.ToString());
    }
}

/// <summary>
/// Contains extension methods for <see cref="OpenTelemetryTracingWorkflowExecutionMiddleware"/>.
/// </summary>
[UsedImplicitly]
public static class OpenTelemetryWorkflowExecutionMiddlewareExtensions
{
    /// Installs the <see cref="OpenTelemetryTracingWorkflowExecutionMiddleware"/> component in the workflow execution pipeline.
    public static IWorkflowExecutionPipelineBuilder UseWorkflowExecutionTracing(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.Insert<OpenTelemetryTracingWorkflowExecutionMiddleware>(0);
}
