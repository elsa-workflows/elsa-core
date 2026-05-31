using Elsa.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Diagnostics.OpenTelemetry.UnitTests.Providers;

public class OpenTelemetryMetricQueryTests
{
    [Fact]
    public async Task QueryMetricsAsync_WhenFilteringByInstrumentName_ReturnsMatchingInstrumentPoints()
    {
        var context = new OpenTelemetryStoreTestContext();
        var resource = context.Resource("resource-api", "api");
        var duration = context.Instrument("instrument-duration", resource.Id, "workflow.duration");
        var count = context.Instrument("instrument-count", resource.Id, "workflow.count");
        await context.WriteAsync(new OpenTelemetryBatch([resource], [], [], [duration, count], [context.Point("point-1", duration.Id, resource.Id), context.Point("point-2", count.Id, resource.Id)], []));

        var result = await context.Store.QueryMetricsAsync(new OpenTelemetryMetricFilter { InstrumentName = "duration" });

        Assert.Equal(duration.Id, Assert.Single(result.Instruments).Id);
        Assert.Equal("point-1", Assert.Single(result.Points).Id);
    }

    [Fact]
    public async Task QueryMetricsAsync_WhenFilteringByInstrumentName_AppliesInstrumentFilterBeforeTake()
    {
        var context = new OpenTelemetryStoreTestContext();
        var resource = context.Resource("resource-api", "api");
        var duration = context.Instrument("instrument-duration", resource.Id, "workflow.duration");
        var count = context.Instrument("instrument-count", resource.Id, "workflow.count");
        await context.WriteAsync(new OpenTelemetryBatch(
            [resource],
            [],
            [],
            [duration, count],
            [
                context.Point("point-1", duration.Id, resource.Id, context.Now),
                context.Point("point-2", count.Id, resource.Id, context.Now.AddSeconds(1))
            ],
            []));

        var result = await context.Store.QueryMetricsAsync(new OpenTelemetryMetricFilter { InstrumentName = "duration", Take = 1 });

        Assert.Equal(duration.Id, Assert.Single(result.Instruments).Id);
        Assert.Equal("point-1", Assert.Single(result.Points).Id);
    }
}
