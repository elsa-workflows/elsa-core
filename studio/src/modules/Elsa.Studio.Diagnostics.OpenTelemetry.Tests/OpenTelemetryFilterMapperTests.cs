using Elsa.Studio.Diagnostics.OpenTelemetry.Models;
using Elsa.Studio.Diagnostics.OpenTelemetry.Services;
using Xunit;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Tests;

public class OpenTelemetryFilterMapperTests
{
    [Fact]
    public void ToTraceRequest_WhenTakeExceedsRowCap_ClampsTake()
    {
        var request = OpenTelemetryFilterMapper.ToTraceRequest(new OpenTelemetryTraceFilter { Take = 500 }, 100);

        Assert.Equal(100, request.Take);
    }

    [Fact]
    public void ToTraceRequest_WhenTraceFiltersAreSet_PreservesFilterValues()
    {
        var from = new DateTimeOffset(2026, 5, 26, 10, 0, 0, TimeSpan.Zero);
        var filter = new OpenTelemetryTraceFilter
        {
            ResourceId = "resource-1",
            ServiceName = "api",
            TraceId = "trace-1",
            WorkflowInstanceId = "workflow-1",
            Status = SpanStatus.Error,
            From = from,
            To = from.AddMinutes(5),
            Search = "approve",
            Take = 25
        };

        var request = OpenTelemetryFilterMapper.ToTraceRequest(filter, 100);

        Assert.Equal("resource-1", request.ResourceId);
        Assert.Equal("api", request.ServiceName);
        Assert.Equal("trace-1", request.TraceId);
        Assert.Equal("workflow-1", request.WorkflowInstanceId);
        Assert.Equal(SpanStatus.Error, request.Status);
        Assert.Equal(from, request.From);
        Assert.Equal(from.AddMinutes(5), request.To);
        Assert.Equal("approve", request.Search);
        Assert.Equal(25, request.Take);
    }

    [Fact]
    public void ToTraceRequest_WhenTakeIsMissing_UsesRowCap()
    {
        var request = OpenTelemetryFilterMapper.ToTraceRequest(new OpenTelemetryTraceFilter(), 75);

        Assert.Equal(75, request.Take);
    }

    [Fact]
    public void ToMetricRequest_WhenTakeIsMissing_UsesRowCap()
    {
        var request = OpenTelemetryFilterMapper.ToMetricRequest(new OpenTelemetryMetricFilter(), 75);

        Assert.Equal(75, request.Take);
    }

    [Fact]
    public void ToLogRequest_WhenRowCapIsInvalid_UsesMinimumTake()
    {
        var request = OpenTelemetryFilterMapper.ToLogRequest(new OpenTelemetryLogFilter(), 0);

        Assert.Equal(1, request.Take);
    }
}
