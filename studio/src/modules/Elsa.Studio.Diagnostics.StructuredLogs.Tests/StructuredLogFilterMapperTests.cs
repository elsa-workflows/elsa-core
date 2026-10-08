using Elsa.Studio.Diagnostics.StructuredLogs.Models;
using Elsa.Studio.Diagnostics.StructuredLogs.Services;
using Xunit;

namespace Elsa.Studio.Diagnostics.StructuredLogs.Tests;

public class StructuredLogFilterMapperTests
{
    private readonly DateTimeOffset _from = DateTimeOffset.UtcNow.AddMinutes(-10);
    private readonly DateTimeOffset _to = DateTimeOffset.UtcNow;
    private readonly StructuredLogFilter _filter;

    public StructuredLogFilterMapperTests()
    {
        _filter = new()
        {
            MinimumLevel = StructuredLogLevel.Warning,
            Levels = [StructuredLogLevel.Error, StructuredLogLevel.Critical],
            CategoryPrefix = "Elsa.Workflows",
            Text = "incident",
            TenantId = "tenant-a",
            WorkflowDefinitionId = "definition-a",
            WorkflowInstanceId = "instance-a",
            TraceId = "trace-a",
            SpanId = "span-a",
            CorrelationId = "correlation-a",
            SourceId = "pod-a",
            From = _from,
            To = _to,
            Take = 50
        };
    }

    [Fact]
    public void ToRecentRequest_MapsFilterFields()
    {
        var request = StructuredLogFilterMapper.ToRecentRequest(_filter, 100);

        Assert.Equal(_filter.MinimumLevel, request.MinimumLevel);
        Assert.Equal(_filter.Levels, request.Levels);
        Assert.Equal(_filter.CategoryPrefix, request.CategoryPrefix);
        Assert.Equal(_filter.Text, request.Text);
        Assert.Equal(_filter.TenantId, request.TenantId);
        Assert.Equal(_filter.WorkflowDefinitionId, request.WorkflowDefinitionId);
        Assert.Equal(_filter.WorkflowInstanceId, request.WorkflowInstanceId);
        Assert.Equal(_filter.TraceId, request.TraceId);
        Assert.Equal(_filter.SpanId, request.SpanId);
        Assert.Equal(_filter.CorrelationId, request.CorrelationId);
        Assert.Equal(_filter.SourceId, request.SourceId);
        Assert.Equal(_from, request.From);
        Assert.Equal(_to, request.To);
        Assert.Equal(50, request.Take);
    }

    [Fact]
    public void ToRecentRequest_ClampsTakeToRowCap()
    {
        var request = StructuredLogFilterMapper.ToRecentRequest(new() { Take = 500 }, 100);

        Assert.Equal(100, request.Take);
    }

    [Fact]
    public void ToRecentRequest_DefaultsTakeToRowCap()
    {
        var request = StructuredLogFilterMapper.ToRecentRequest(new(), 100);

        Assert.Equal(100, request.Take);
    }

    [Fact]
    public void ToLiveSubscription_PreservesSourceFilter()
    {
        var request = StructuredLogFilterMapper.ToLiveSubscription(new() { SourceId = "pod-a" });

        Assert.Equal("pod-a", request.SourceId);
    }

    [Fact]
    public void ToLiveSubscription_CopiesLevelCollection()
    {
        _filter.Levels = [StructuredLogLevel.Warning];
        var request = StructuredLogFilterMapper.ToLiveSubscription(_filter);

        _filter.Levels.Add(StructuredLogLevel.Error);

        Assert.Equal([StructuredLogLevel.Warning], request.Levels);
    }

    [Fact]
    public void ToLiveSubscription_PreservesSpanFilter()
    {
        var request = StructuredLogFilterMapper.ToLiveSubscription(_filter);

        Assert.Equal("span-a", request.SpanId);
    }
}
