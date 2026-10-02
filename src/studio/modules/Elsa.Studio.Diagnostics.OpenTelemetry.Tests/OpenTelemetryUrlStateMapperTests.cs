using Elsa.Studio.Diagnostics.OpenTelemetry.Models;
using Elsa.Studio.Diagnostics.OpenTelemetry.Services;
using Xunit;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Tests;

public class OpenTelemetryUrlStateMapperTests
{
    [Fact]
    public void ToQuery_WhenStateHasValues_WritesStableQueryKeys()
    {
        var from = new DateTimeOffset(2026, 5, 26, 10, 0, 0, TimeSpan.Zero);
        var state = new OpenTelemetryUrlState
        {
            Tab = "Traces",
            ResourceId = "api:1",
            ServiceName = "api",
            TraceId = "trace-1",
            SpanId = "span-1",
            WorkflowInstanceId = "wf-1",
            WorkflowDefinitionId = "orders",
            Severity = "Error",
            Status = SpanStatus.Error,
            Text = "failed",
            From = from,
            To = from.AddMinutes(5),
            Live = true
        };

        var query = OpenTelemetryUrlStateMapper.ToQuery(state);

        Assert.Equal("Traces", query["tab"]);
        Assert.Equal("api:1", query["resource"]);
        Assert.Equal("orders", query["definition"]);
        Assert.Equal("Error", query["status"]);
        Assert.Equal("true", query["live"]);
    }

    [Fact]
    public void FromQuery_WhenQueryHasValues_RestoresState()
    {
        var state = OpenTelemetryUrlStateMapper.FromQuery(new Dictionary<string, string?>
        {
            ["tab"] = "Logs",
            ["resource"] = "api:1",
            ["service"] = "api",
            ["trace"] = "trace-1",
            ["span"] = "span-1",
            ["workflow"] = "wf-1",
            ["definition"] = "orders",
            ["severity"] = "Warning",
            ["status"] = "Error",
            ["text"] = "failed",
            ["from"] = "2026-05-26T10:00:00.0000000+00:00",
            ["to"] = "2026-05-26T10:05:00.0000000+00:00",
            ["live"] = "false"
        });

        Assert.Equal("Logs", state.Tab);
        Assert.Equal("api:1", state.ResourceId);
        Assert.Equal("trace-1", state.TraceId);
        Assert.Equal(SpanStatus.Error, state.Status);
        Assert.False(state.Live);
        Assert.Equal(new DateTimeOffset(2026, 5, 26, 10, 0, 0, TimeSpan.Zero), state.From);
    }

    [Fact]
    public void ToTraceFilter_WhenStateHasTraceValues_AppliesRowCap()
    {
        var filter = OpenTelemetryUrlStateMapper.ToTraceFilter(new OpenTelemetryUrlState
        {
            TraceId = "trace-1",
            WorkflowInstanceId = "wf-1",
            Status = SpanStatus.Ok,
            Text = "approve"
        }, 50);

        Assert.Equal("trace-1", filter.TraceId);
        Assert.Equal("wf-1", filter.WorkflowInstanceId);
        Assert.Equal(SpanStatus.Ok, filter.Status);
        Assert.Equal("approve", filter.Search);
        Assert.Equal(50, filter.Take);
    }
}
