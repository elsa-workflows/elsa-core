using Elsa.ServerLogs.Models;
using Elsa.ServerLogs.Services;

namespace Elsa.ServerLogs.UnitTests.Filtering;

public class ServerLogFilterTests
{
    private readonly ServerLogEvent _logEvent = new()
    {
        Timestamp = new DateTimeOffset(2026, 5, 7, 10, 0, 0, TimeSpan.Zero),
        Level = ServerLogLevel.Warning,
        Category = "Elsa.Workflows.Runtime",
        Message = "Workflow instance faulted",
        Exception = new("System.Exception", "Storage timeout", null),
        TenantId = "tenant-a",
        WorkflowInstanceId = "workflow-instance-a",
        TraceId = "trace-a",
        CorrelationId = "correlation-a",
        SourceId = "pod-a"
    };

    [Fact]
    public void Matches_WhenAllCriteriaMatch_ReturnsTrue()
    {
        var filter = new ServerLogFilter
        {
            MinimumLevel = ServerLogLevel.Information,
            CategoryPrefix = "Elsa.Workflows",
            Text = "faulted",
            TenantId = "tenant-a",
            WorkflowInstanceId = "workflow-instance-a",
            TraceId = "trace-a",
            CorrelationId = "correlation-a",
            SourceId = "pod-a",
            From = _logEvent.Timestamp.AddMinutes(-1),
            To = _logEvent.Timestamp.AddMinutes(1)
        };

        Assert.True(ServerLogFilterEvaluator.Matches(_logEvent, filter));
    }

    [Fact]
    public void Matches_WhenTextMatchesException_ReturnsTrue()
    {
        Assert.True(ServerLogFilterEvaluator.Matches(_logEvent, new()
        {
            Text = "timeout"
        }));
    }

    [Fact]
    public void Matches_WhenSourceDoesNotMatch_ReturnsFalse()
    {
        Assert.False(ServerLogFilterEvaluator.Matches(_logEvent, new()
        {
            SourceId = "pod-b"
        }));
    }
}
