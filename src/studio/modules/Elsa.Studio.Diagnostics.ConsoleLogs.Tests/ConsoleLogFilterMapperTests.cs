using Elsa.Studio.Diagnostics.ConsoleLogs.Models;
using Elsa.Studio.Diagnostics.ConsoleLogs.Services;
using Xunit;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Tests;

public class ConsoleLogFilterMapperTests
{
    private readonly DateTimeOffset _from = DateTimeOffset.UtcNow.AddMinutes(-10);
    private readonly DateTimeOffset _to = DateTimeOffset.UtcNow;
    private readonly ConsoleLogFilter _filter;

    public ConsoleLogFilterMapperTests()
    {
        _filter = new()
        {
            SourceId = "source-a",
            Stream = ConsoleLogStream.Stdout,
            Query = "incident",
            WorkflowInstanceId = "workflow-instance-a",
            ActivityInstanceId = "activity-instance-a",
            ActivityId = "activity-a",
            ActivityNodeId = "node-a",
            From = _from,
            To = _to,
            Limit = 50
        };
    }

    [Fact]
    public void ToRecentRequest_MapsFilterFields()
    {
        var request = ConsoleLogFilterMapper.ToRecentRequest(_filter, 100);

        Assert.Equal(_filter.SourceId, request.SourceId);
        Assert.Equal(_filter.Stream, request.Stream);
        Assert.Equal(_filter.Query, request.Query);
        Assert.Equal(_filter.WorkflowInstanceId, request.WorkflowInstanceId);
        Assert.Equal(_filter.ActivityInstanceId, request.ActivityInstanceId);
        Assert.Equal(_filter.ActivityId, request.ActivityId);
        Assert.Equal(_filter.ActivityNodeId, request.ActivityNodeId);
        Assert.Equal(_from, request.From);
        Assert.Equal(_to, request.To);
        Assert.Equal(50, request.Limit);
    }

    [Fact]
    public void ToRecentRequest_ClampsLimitToRowCap()
    {
        var request = ConsoleLogFilterMapper.ToRecentRequest(new() { Limit = 500 }, 100);

        Assert.Equal(100, request.Limit);
    }

    [Fact]
    public void ToRecentRequest_DefaultsLimitToRowCap()
    {
        var request = ConsoleLogFilterMapper.ToRecentRequest(new(), 100);

        Assert.Equal(100, request.Limit);
    }

    [Fact]
    public void ToLiveSubscription_PreservesStream()
    {
        var request = ConsoleLogFilterMapper.ToLiveSubscription(_filter);

        Assert.Equal(_filter.Stream, request.Stream);
    }

    [Fact]
    public void ToLiveSubscription_PreservesQueryFilter()
    {
        var request = ConsoleLogFilterMapper.ToLiveSubscription(new() { Query = " raw value " });

        Assert.Equal(" raw value ", request.Query);
    }

    [Fact]
    public void ToLiveSubscription_PreservesWorkflowInstanceScope()
    {
        var request = ConsoleLogFilterMapper.ToLiveSubscription(new() { WorkflowInstanceId = "workflow-instance-a" });

        Assert.Equal("workflow-instance-a", request.WorkflowInstanceId);
    }

    [Fact]
    public void ToLiveSubscription_PreservesActivityScope()
    {
        var request = ConsoleLogFilterMapper.ToLiveSubscription(new()
        {
            ActivityInstanceId = "activity-instance-a",
            ActivityId = "activity-a",
            ActivityNodeId = "node-a"
        });

        Assert.Equal("activity-instance-a", request.ActivityInstanceId);
        Assert.Equal("activity-a", request.ActivityId);
        Assert.Equal("node-a", request.ActivityNodeId);
    }
}
