using System.Diagnostics;
using System.Text.Json;
using Elsa.Common.Contracts;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.OpenTelemetry.Helpers;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Serialization.Converters;
using JetBrains.Annotations;
using Activity = System.Diagnostics.Activity;
using ActivityKind = System.Diagnostics.ActivityKind;

namespace Elsa.OpenTelemetry.Middleware;

/// <summary>
/// Middleware that traces workflow execution using OpenTelemetry.
/// </summary>
[UsedImplicitly]
public class OpenTelemetryTracingWorkflowExecutionMiddleware(WorkflowMiddlewareDelegate next, ISystemClock systemClock) : WorkflowExecutionMiddleware(next)
{
    private readonly JsonSerializerOptions? _incidentSerializerOptions = new JsonSerializerOptions().WithConverters(new TypeJsonConverter(WellKnownTypeRegistry.CreateDefault()));

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var workflowInstanceId = context.Id;
        var workflow = context.Workflow;
        using var activity = ElsaOpenTelemetry.ActivitySource.StartActivity($"WorkflowExecution", ActivityKind.Internal, Activity.Current?.Context ?? default);

        if (activity == null)
        {
            await Next(context);
            return;
        }

        if (!string.IsNullOrWhiteSpace(context.CorrelationId))
            activity.SetTag("correlationId", context.CorrelationId);

        activity.SetTag("workflowInstance.id", workflowInstanceId);
        activity.SetTag("workflowDefinition.definitionId", workflow.Identity.DefinitionId);
        activity.SetTag("workflowDefinition.version", workflow.Identity.Version);
        activity.SetTag("workflowDefinition.name", workflow.WorkflowMetadata.Name);
        activity.AddEvent(new ActivityEvent("Executing", tags: new ActivityTagsCollection(new Dictionary<string, object?>
        {
            ["workflowInstance.status"] = context.Status.ToString(),
            ["workflowInstance.subStatus"] = context.SubStatus.ToString()
        })));
        await Next(context);

        if (context.SubStatus == WorkflowSubStatus.Faulted)
        {
            activity.AddEvent(new ActivityEvent("Faulted"));
            activity.SetStatus(ActivityStatusCode.Error);
            activity.SetTag("error", true);
        }
        else
        {
            activity.AddEvent(new ActivityEvent("Executed", tags: new ActivityTagsCollection(new Dictionary<string, object?>
            {
                ["workflowInstance.status"] = context.Status.ToString(),
                ["workflowInstance.subStatus"] = context.SubStatus.ToString()
            })));
        }
        
        if(context.Incidents.Any())
        {
            activity.SetStatus(ActivityStatusCode.Error);
            activity.SetTag("hasIncidents", true);
            activity.SetTag("error", true);

            if (context.Incidents.Count > 0)
                activity.SetTag("error.message", JsonSerializer.Serialize(context.Incidents, _incidentSerializerOptions));
        }
        
        if (!string.IsNullOrWhiteSpace(context.CorrelationId))
            activity.SetTag("correlationId", context.CorrelationId);
        
        activity.SetTag("workflowExecution.durationMs", (systemClock.UtcNow - activity.StartTimeUtc).TotalMilliseconds);
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