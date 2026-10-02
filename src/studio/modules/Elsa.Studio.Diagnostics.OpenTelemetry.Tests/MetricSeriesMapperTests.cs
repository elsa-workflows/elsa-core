using Elsa.Studio.Diagnostics.OpenTelemetry.Models;
using Elsa.Studio.Diagnostics.OpenTelemetry.Services;
using Xunit;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Tests;

public class MetricSeriesMapperTests
{
    private readonly DateTimeOffset _now = new(2026, 5, 26, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateRows_WhenPointsExist_UsesLatestPointPerInstrument()
    {
        var instrument = Instrument("duration", "workflow.duration");
        var metrics = new OpenTelemetryMetricResult(
            [instrument],
            [
                Point("point-1", instrument.Id, _now, 10),
                Point("point-2", instrument.Id, _now.AddMinutes(1), 25)
            ],
            0);

        var row = Assert.Single(MetricSeriesMapper.CreateRows(metrics));

        Assert.Equal(2, row.PointCount);
        Assert.Equal(_now.AddMinutes(1), row.LastTimestamp);
        Assert.Equal(25, row.LastValue);
    }

    [Fact]
    public void CreateRows_WhenInstrumentHasNoPoints_ReturnsEmptyPointSummary()
    {
        var instrument = Instrument("active", "workflow.active");

        var row = Assert.Single(MetricSeriesMapper.CreateRows(new OpenTelemetryMetricResult([instrument], [], 0)));

        Assert.Equal(0, row.PointCount);
        Assert.Null(row.LastTimestamp);
        Assert.Null(row.LastValue);
    }

    [Fact]
    public void CreateRows_WhenMultipleInstrumentsExist_SortsByName()
    {
        var second = Instrument("second", "z.metric");
        var first = Instrument("first", "a.metric");

        var rows = MetricSeriesMapper.CreateRows(new OpenTelemetryMetricResult([second, first], [], 0)).ToArray();

        Assert.Equal("a.metric", rows[0].Instrument.Name);
        Assert.Equal("z.metric", rows[1].Instrument.Name);
    }

    private static MetricInstrument Instrument(string id, string name)
    {
        return new(id, "resource-1", name, "ms", null, MetricKind.Gauge, new Dictionary<string, string?>());
    }

    private static MetricPoint Point(string id, string instrumentId, DateTimeOffset timestamp, double value)
    {
        return new(id, instrumentId, instrumentId, "resource-1", timestamp, value, null, null, new Dictionary<string, string?>(), null, null);
    }
}
