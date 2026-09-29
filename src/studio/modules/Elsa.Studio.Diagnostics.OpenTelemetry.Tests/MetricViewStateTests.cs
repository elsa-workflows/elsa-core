using Elsa.Studio.Diagnostics.OpenTelemetry.Services;
using Xunit;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Tests;

public class MetricViewStateTests
{
    private readonly DateTimeOffset _from = new(2026, 5, 26, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ToQuery_WhenMetricsTabIsSelected_PreservesMetricViewState()
    {
        var query = OpenTelemetryUrlStateMapper.ToQuery(new OpenTelemetryUrlState
        {
            Tab = "Metrics",
            ResourceId = "api:1",
            ServiceName = "api",
            Text = "workflow.duration",
            From = _from,
            To = _from.AddMinutes(15),
            Live = true
        });

        Assert.Equal("Metrics", query["tab"]);
        Assert.Equal("api:1", query["resource"]);
        Assert.Equal("api", query["service"]);
        Assert.Equal("workflow.duration", query["text"]);
        Assert.Equal("true", query["live"]);
    }

    [Fact]
    public void FromQuery_WhenMetricsTabQueryIsProvided_RestoresMetricViewState()
    {
        var state = OpenTelemetryUrlStateMapper.FromQuery(new Dictionary<string, string?>
        {
            ["tab"] = "Metrics",
            ["resource"] = "api:1",
            ["service"] = "api",
            ["text"] = "workflow.duration",
            ["from"] = "2026-05-26T10:00:00.0000000+00:00",
            ["to"] = "2026-05-26T10:15:00.0000000+00:00",
            ["live"] = "true"
        });

        Assert.Equal("Metrics", state.Tab);
        Assert.Equal("api:1", state.ResourceId);
        Assert.Equal("api", state.ServiceName);
        Assert.Equal("workflow.duration", state.Text);
        Assert.Equal(_from, state.From);
        Assert.Equal(_from.AddMinutes(15), state.To);
        Assert.True(state.Live);
    }

    [Fact]
    public void ToMetricFilter_WhenStateHasMetricValues_AppliesRowCap()
    {
        var filter = OpenTelemetryUrlStateMapper.ToMetricFilter(new OpenTelemetryUrlState
        {
            ResourceId = "api:1",
            ServiceName = "api",
            Text = "workflow.duration",
            From = _from,
            To = _from.AddMinutes(15)
        }, 50);

        Assert.Equal("api:1", filter.ResourceId);
        Assert.Equal("api", filter.ServiceName);
        Assert.Equal("workflow.duration", filter.InstrumentName);
        Assert.Equal(_from, filter.From);
        Assert.Equal(_from.AddMinutes(15), filter.To);
        Assert.Equal(50, filter.Take);
    }
}
