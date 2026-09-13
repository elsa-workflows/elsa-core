using Elsa.Diagnostics.OpenTelemetry.Models;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.OpenTelemetry.UnitTests.Providers;

public class OpenTelemetryMetricQueryTests
{
    [Test]
    public async Task QueryMetricsAsync_WhenFilteringByInstrumentName_ReturnsMatchingInstrumentPoints()
    {
        var context = new OpenTelemetryStoreTestContext();
        var resource = context.Resource("resource-api", "api");
        var duration = context.Instrument("instrument-duration", resource.Id, "workflow.duration");
        var count = context.Instrument("instrument-count", resource.Id, "workflow.count");
        await context.WriteAsync(new OpenTelemetryBatch([resource], [], [], [duration, count], [context.Point("point-1", duration.Id, resource.Id), context.Point("point-2", count.Id, resource.Id)], []));

        var result = await context.Store.QueryMetricsAsync(new OpenTelemetryMetricFilter { InstrumentName = "duration" });

        var instrument = await Assert.That(result.Instruments).HasSingleItem();
        var point = await Assert.That(result.Points).HasSingleItem();
        await Assert.That(instrument.Id).IsEqualTo(duration.Id);
        await Assert.That(point.Id).IsEqualTo("point-1");
    }

    [Test]
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

        var instrument = await Assert.That(result.Instruments).HasSingleItem();
        var point = await Assert.That(result.Points).HasSingleItem();
        await Assert.That(instrument.Id).IsEqualTo(duration.Id);
        await Assert.That(point.Id).IsEqualTo("point-1");
    }
}
