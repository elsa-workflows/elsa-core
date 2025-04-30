using System.Diagnostics.Metrics;
using Elsa.OpenTelemetry.Contracts;
using Elsa.OpenTelemetry.Models;
using Elsa.Workflows;

namespace Elsa.OpenTelemetry.Metrics;

/// Tracks and records metrics related to workflow incidents.
public class ErrorMetrics
{
    private readonly IEnumerable<IErrorMetricHandler> _errorMetricHandlers;
    private readonly Counter<long> _errorCounter;

    /// Tracks and records metrics related to workflow incidents.
    public ErrorMetrics(IMeterFactory meterFactory, IEnumerable<IErrorMetricHandler> errorMetricHandlers)
    {
        var meter = meterFactory.Create(MeterName);
        _errorMetricHandlers = errorMetricHandlers;
        _errorCounter = meter.CreateCounter<long>(
            name: "workflow_incident_count",
            description: "Counts workflow incidents"
        );
    }

    public const string MeterName = "Elsa.OpenTelemetry.Incidents";

    /// Track 
    public void TrackError(ActivityExecutionContext context)
    {
        var exception = context.Exception;

        if (exception == null)
            return;

        var workflow = context.WorkflowExecutionContext.Workflow;
        var tags = new Dictionary<string, object?>()
        {
            ["activity.type"] = context.Activity.Type,
            ["workflow.definition.id"] = workflow.Identity.DefinitionId,
            ["workflow.definition.version"] = workflow.Identity.Version
        };

        if (!string.IsNullOrWhiteSpace(workflow.WorkflowMetadata.Name))
            tags["workflow.definition.name"] = workflow.WorkflowMetadata.Name;

        if (!string.IsNullOrWhiteSpace(workflow.Identity.TenantId))
            tags["tenant.id"] = workflow.Identity.TenantId;

        var errorMetricContext = new ErrorMetricContext(_errorCounter, exception, tags);
        var errorMetricHandlers = _errorMetricHandlers
            .OrderBy(x => x.Order)
            .Where(x => x.CanHandle(errorMetricContext));

        foreach (var handler in errorMetricHandlers) 
            handler.Handle(errorMetricContext);
        
        _errorCounter.Add(1, tags.ToArray());
    }
}