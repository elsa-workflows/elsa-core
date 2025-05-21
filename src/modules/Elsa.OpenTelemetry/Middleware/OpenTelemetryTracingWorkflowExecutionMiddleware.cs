using System.Diagnostics;
using Elsa.Common;
using Elsa.OpenTelemetry.Contracts;
using Elsa.OpenTelemetry.Helpers;
using Elsa.OpenTelemetry.Models;
using Elsa.OpenTelemetry.Options;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Activity = System.Diagnostics.Activity;
using ActivityKind = System.Diagnostics.ActivityKind;

namespace Elsa.OpenTelemetry.Middleware;

/// <summary>
/// Middleware that traces workflow execution using OpenTelemetry.
/// </summary>
[UsedImplicitly]
public class OpenTelemetryTracingWorkflowExecutionMiddleware(WorkflowMiddlewareDelegate next, IOptions<OpenTelemetryOptions> options) : WorkflowExecutionMiddleware(next)
{
    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var workflowName = context.Workflow.WorkflowMetadata.Name;
        var workflowInstanceId = context.Id;
        var workflow = context.Workflow;

        using var span = CreateTraceActivity(context, workflowName);

        if (span == null)
        {
            await Next(context);
            return;
        }

        span.SetTag("operation.name", "elsa.workflow.execution");
        span.SetTag("span.type", "workflow");
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

            var lastIncident = context.Incidents.FirstOrDefault();

            if (lastIncident == null)
                span.SetStatus(ActivityStatusCode.Error, "The workflow entered the Faulted state. See incidents for details.");
            else
            {
                span.SetStatus(ActivityStatusCode.Error, lastIncident.Message);

                var activityExecutionContext = context.ActivityExecutionContexts.FirstOrDefault(x => x.Activity.NodeId == lastIncident.ActivityNodeId && x.Status == ActivityStatus.Faulted);
                var exception = activityExecutionContext?.Exception;

                if (exception != null)
                {
                    var errorSpanHandlerContext = new WorkflowErrorSpanContext(span, lastIncident, exception);
                    var errorSpanHandler = context.GetServices<IWorkflowErrorSpanHandler>()
                        .OrderBy(x => x.Order)
                        .FirstOrDefault(x => x.CanHandle(errorSpanHandlerContext));

                    errorSpanHandler?.Handle(errorSpanHandlerContext);
                }
            }
        }
        else if (context.SubStatus == WorkflowSubStatus.Finished)
        {
            span.AddEvent(new("finished"));
        }
        else if (context.SubStatus == WorkflowSubStatus.Cancelled)
        {
            span.AddEvent(new("canceled"));
        }
        else if (context.SubStatus == WorkflowSubStatus.Suspended)
        {
            span.AddEvent(new("suspended"));
        }

        if (context.Incidents.Any())
        {
            span.SetTag("workflow.incidents.count", context.Incidents.Count);

            foreach (var incident in context.Incidents)
                span.AddEvent(new("incident", incident.Timestamp, CreateIncidentTags(incident)));
        }

        if (!string.IsNullOrWhiteSpace(context.CorrelationId))
            span.SetTag("workflow.correlation_id", context.CorrelationId);
    }

    private Activity? CreateTraceActivity(WorkflowExecutionContext context, string? workflowName)
    {
        var startNewTraceOptionValue = context.Properties.TryGetValue("StartNewTrace", out var startNewTraceValue) && (bool)startNewTraceValue;
        var startNewTraceForRemoteParent = options.Value.UseNewRootActivityForRemoteParent && Activity.Current?.HasRemoteParent == true;
        var startNewTrace = startNewTraceOptionValue || startNewTraceForRemoteParent;

        ActivityContext contextToUse;
        ActivityContext? linkedTraceContext = null;

        if (startNewTrace)
        {
            linkedTraceContext = Activity.Current?.Context;
            Activity.Current?.Stop();
            Activity.Current = null;

            contextToUse = options.Value.UseDummyParentActivityAsRootSpan
                ? new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded)
                : default;
        }
        else
        {
            contextToUse = Activity.Current?.Context ?? default;
        }

        var span = ElsaOpenTelemetry.ActivitySource.StartActivity($"execute workflow {workflowName}", ActivityKind.Server, contextToUse);

        if (span != null && linkedTraceContext != null)
            span.AddLink(new(linkedTraceContext.Value));

        return span;
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