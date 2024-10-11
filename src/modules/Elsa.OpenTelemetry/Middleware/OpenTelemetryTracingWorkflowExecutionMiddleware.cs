using System.Diagnostics;
using System.Text.Json;
using Elsa.Common;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.OpenTelemetry.Helpers;
using Elsa.Workflows;
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
        using var span = ElsaOpenTelemetry.ActivitySource.StartActivity("WorkflowExecution", ActivityKind.Internal, Activity.Current?.Context ?? default);

        if (span == null) // No listener is registered.
        {
            await Next(context);
            return;
        }

        span.SetTag("workflowInstance.id", workflowInstanceId);
        span.SetTag("workflowDefinition.definitionId", workflow.Identity.DefinitionId);
        span.SetTag("workflowDefinition.version", workflow.Identity.Version);
        span.SetTag("workflowDefinition.name", workflow.WorkflowMetadata.Name);
        span.SetTag("workflowExecution.startTimeUtc", span.StartTimeUtc);
        
        if(context.TriggerActivityId != null)
        {
            var activity = context.FindActivityById(context.TriggerActivityId) ?? throw new Exception($"Trigger activity with ID {context.TriggerActivityId} not found. This should not happen.");
            span.SetTag("workflowExecution.trigger.activityId", activity.Id);
            span.SetTag("workflowExecution.trigger.activityName", activity.Name);
            span.SetTag("workflowExecution.trigger.activityType", activity.Type);
        }
        
        span.AddEvent(new ActivityEvent("Executing", tags: CreateStatusTags(context)));
        await Next(context);

        if (context.SubStatus == WorkflowSubStatus.Faulted)
        {
            span.AddEvent(new ActivityEvent("Faulted", tags: CreateStatusTags(context)));
            span.SetStatus(ActivityStatusCode.Error);
            span.SetTag("error", true);
        }
        else
        {
            span.AddEvent(new ActivityEvent("Executed", tags: CreateStatusTags(context)));
            span.SetStatus(ActivityStatusCode.Ok);
        }
        
        if(context.Incidents.Any())
        {
            span.SetStatus(ActivityStatusCode.Error);
            span.SetTag("workflowInstance.hasIncidents", true);
            span.SetTag("error", true);

            if (context.Incidents.Count > 0)
                span.SetTag("error.message", JsonSerializer.Serialize(context.Incidents, _incidentSerializerOptions));
        }
        
        if (!string.IsNullOrWhiteSpace(context.CorrelationId))
            span.SetTag("workflowInstance.correlationId", context.CorrelationId);
        
        var now = systemClock.UtcNow;
        span.SetTag("workflowExecution.endTimeUtc", now);
        span.SetTag("workflowExecution.durationMs", (now - span.StartTimeUtc).TotalMilliseconds);
    }
    
    private ActivityTagsCollection CreateStatusTags(WorkflowExecutionContext context)
    {
        return new ActivityTagsCollection(new Dictionary<string, object?>
        {
            ["workflowInstance.status"] = context.Status.ToString(),
            ["workflowInstance.subStatus"] = context.SubStatus.ToString()
        });
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