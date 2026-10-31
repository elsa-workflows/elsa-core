using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Services;

namespace Elsa.Diagnostics.StructuredLogs.UnitTests.Filtering;

public class StructuredLogFilterTests
{
    private readonly StructuredLogEvent _logEvent = new()
    {
        Timestamp = new DateTimeOffset(2026, 5, 7, 10, 0, 0, TimeSpan.Zero),
        Level = StructuredLogLevel.Warning,
        Category = "Elsa.Workflows.Runtime",
        Message = "Workflow instance faulted",
        MessageTemplate = "Workflow instance {WorkflowInstanceId} faulted",
        Exception = new("System.Exception", "Storage timeout", null),
        Scopes = new Dictionary<string, string?> { ["TenantId"] = "tenant-a" },
        Properties = new Dictionary<string, string?> { ["WorkflowInstanceId"] = "workflow-instance-a" },
        TenantId = "tenant-a",
        WorkflowInstanceId = "workflow-instance-a",
        TraceId = "trace-a",
        SpanId = "span-a",
        CorrelationId = "correlation-a",
        SourceId = "pod-a"
    };

    [Fact]
    public void Matches_WhenAllCriteriaMatch_ReturnsTrue()
    {
        var filter = new StructuredLogFilter
        {
            MinimumLevel = StructuredLogLevel.Information,
            CategoryPrefix = "Elsa.Workflows",
            Text = "faulted",
            TenantId = "tenant-a",
            WorkflowInstanceId = "workflow-instance-a",
            TraceId = "trace-a",
            SpanId = "span-a",
            CorrelationId = "correlation-a",
            SourceId = "pod-a",
            From = _logEvent.Timestamp.AddMinutes(-1),
            To = _logEvent.Timestamp.AddMinutes(1)
        };

        Assert.True(StructuredLogFilterEvaluator.Matches(_logEvent, filter));
    }

    [Fact]
    public void Matches_WhenTextMatchesException_ReturnsTrue()
    {
        Assert.True(StructuredLogFilterEvaluator.Matches(_logEvent, new()
        {
            Text = "timeout"
        }));
    }

    [Fact]
    public void Matches_WhenTextMatchesMessageTemplateOrStructuredValue_ReturnsTrue()
    {
        Assert.True(StructuredLogFilterEvaluator.Matches(_logEvent, new()
        {
            Text = "WorkflowInstanceId"
        }));

        Assert.True(StructuredLogFilterEvaluator.Matches(_logEvent, new()
        {
            Text = "tenant-a"
        }));
    }

    [Fact]
    public void Matches_WhenSourceDoesNotMatch_ReturnsFalse()
    {
        Assert.False(StructuredLogFilterEvaluator.Matches(_logEvent, new()
        {
            SourceId = "pod-b"
        }));
    }
}
