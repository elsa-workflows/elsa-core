using System.Diagnostics;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;
using Elsa.Common;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.OpenTelemetry.Helpers;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
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
        using var span = ElsaOpenTelemetry.ActivitySource.StartActivity($"execute workflow {workflow.WorkflowMetadata.Name}", ActivityKind.Server, Activity.Current?.Context ?? default);
        var now = systemClock.UtcNow;

        if (span == null) // No listener is registered.
        {
            await Next(context);
            return;
        }

        span.SetTag("operation.name", "elsa.workflow.execution");
        span.SetTag("workflow.definition.id", workflow.Identity.DefinitionId);
        span.SetTag("workflow.definition.version", workflow.Identity.Version);
        span.SetTag("workflow.definition.name", workflow.WorkflowMetadata.Name);
        span.SetTag("workflow.definition.tenant.id", workflow.Identity.TenantId);
        span.SetTag("workflow.instance.id", workflowInstanceId);

        if (context.TriggerActivityId != null)
        {
            var activity = context.FindActivityById(context.TriggerActivityId) ?? throw new($"Trigger activity with ID {context.TriggerActivityId} not found. This should not happen.");
            span.SetTag("workflow.trigger.activity.id", activity.Id);
            span.SetTag("workflow.trigger.activity.name", activity.Name);
            span.SetTag("workflow.trigger.activity.type", activity.Type);
            span.SetTag("workflow.trigger.activity.version", activity.Version);
        }

        span.AddEvent(new("executing"));
        await Next(context);

        if (context.SubStatus == WorkflowSubStatus.Faulted)
        {
            span.AddEvent(new("faulted"));
            span.SetStatus(ActivityStatusCode.Error, "The workflow entered the Faulted state. See incidents for details.");
        }
        else if (context.SubStatus == WorkflowSubStatus.Finished)
        {
            span.AddEvent(new("finished"));
            span.SetStatus(ActivityStatusCode.Ok);
        }
        else if (context.SubStatus == WorkflowSubStatus.Cancelled)
        {
            span.AddEvent(new("canceled"));
            span.SetStatus(ActivityStatusCode.Ok);
        }
        else if (context.SubStatus == WorkflowSubStatus.Suspended)
        {
            span.AddEvent(new("suspended"));
            span.SetStatus(ActivityStatusCode.Ok);
        }
        else if (context.SubStatus == WorkflowSubStatus.Pending)
        {
            span.AddEvent(new("pending"));
            span.SetStatus(ActivityStatusCode.Ok);
        }

        if (context.Incidents.Any())
        {
            span.SetTag("workflow.incidents", true);
            span.SetTag("workflow.incidents.count", context.Incidents.Count);

            foreach (var incident in context.Incidents)
                span.AddEvent(new("incident", incident.Timestamp, CreateIncidentTags(incident)));
        }

        if (!string.IsNullOrWhiteSpace(context.CorrelationId))
            span.SetTag("workflow.correlation_id", context.CorrelationId);
    }

    private ActivityTagsCollection CreateIncidentTags(ActivityIncident incident)
    {
        var tags = new ActivityTagsCollection(new Dictionary<string, object?>
        {
            ["incident.message"] = incident.Message,
        });

        if (incident.Exception != null)
        {
            tags["incident.exception.message"] = incident.Exception.Message;
            tags["incident.exception.stackTrace"] = incident.Exception.StackTrace;
            tags["incident.exception.type"] = incident.Exception.Type.FullName;
        }

        return tags;
    }
}

/// <summary>
/// Contains extension methods for <see cref="OpenTelemetryTracingWorkflowExecutionMiddleware"/>.
/// </summary>
[UsedImplicitly]
public static class OpenTelemetryWorkflowExecutionMiddlewareExtensions
{
    /// <summary>
    /// Installs the <see cref="OpenTelemetryTracingWorkflowExecutionMiddleware"/> component in the workflow execution pipeline.
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UseWorkflowExecutionTracing(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.Insert<OpenTelemetryTracingWorkflowExecutionMiddleware>(0);
}