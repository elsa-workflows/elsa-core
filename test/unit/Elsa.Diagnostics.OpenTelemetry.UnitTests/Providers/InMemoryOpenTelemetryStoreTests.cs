using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Options;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.OpenTelemetry.UnitTests.Providers;

public class InMemoryOpenTelemetryStoreTests
{
    [Test]
    public async Task WriteAsync_WhenTraceCapacityIsExceeded_DropsOldestTraceAndReportsCount()
    {
        var context = new OpenTelemetryStoreTestContext(new OpenTelemetryDiagnosticsOptions
        {
            TraceCapacity = 2,
            SpanCapacity = 10,
            MetricPointCapacity = 10,
            LogRecordCapacity = 10
        });
        var resource = context.Resource("resource-a", "api");

        await context.WriteAsync(new OpenTelemetryBatch([resource], [context.Trace("trace-1", resource.Id, context.Now.AddSeconds(1))], [], [], [], []));
        await context.WriteAsync(new OpenTelemetryBatch([], [context.Trace("trace-2", resource.Id, context.Now.AddSeconds(2))], [], [], [], []));
        await context.WriteAsync(new OpenTelemetryBatch([], [context.Trace("trace-3", resource.Id, context.Now.AddSeconds(3))], [], [], [], []));

        var result = await context.Store.QueryTracesAsync(new OpenTelemetryTraceFilter { Take = 10 });
        var diagnostics = await context.Store.GetDiagnosticsAsync();

        await Assert.That(result.Items.Select(x => x.TraceId)).IsEquivalentTo(["trace-2", "trace-3"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(result.DroppedCount).IsEqualTo(1);
        await Assert.That(diagnostics.DroppedTraceCount).IsEqualTo(1);
        await Assert.That(diagnostics.TraceCount).IsEqualTo(2);
    }

    [Test]
    public async Task QueryResourcesAsync_WhenTakeExceedsMaxQuerySize_ClampsResult()
    {
        var context = new OpenTelemetryStoreTestContext(new OpenTelemetryDiagnosticsOptions { MaxQuerySize = 1 });

        await context.WriteAsync(new OpenTelemetryBatch([context.Resource("resource-a", "api"), context.Resource("resource-b", "worker")], [], [], [], [], []));

        var result = await context.Store.QueryResourcesAsync(new OpenTelemetryResourceFilter { Take = 10 });

        await Assert.That(result.Items).HasSingleItem();
    }

    [Test]
    public async Task QueryTracesAsync_WhenTraceIdAppearsInMultipleBatches_ReturnsLatestTrace()
    {
        var context = new OpenTelemetryStoreTestContext(new OpenTelemetryDiagnosticsOptions { TraceCapacity = 10 });
        var resource = context.Resource("resource-a", "api");

        await context.WriteAsync(new OpenTelemetryBatch([resource], [context.Trace("trace-1", resource.Id, context.Now.AddSeconds(1))], [], [], [], []));
        await context.WriteAsync(new OpenTelemetryBatch([], [context.Trace("trace-1", resource.Id, context.Now.AddSeconds(2))], [], [], [], []));

        var result = await context.Store.QueryTracesAsync(new OpenTelemetryTraceFilter { Take = 10 });

        var trace = await Assert.That(result.Items).HasSingleItem();
        await Assert.That(trace.StartTime).IsEqualTo(context.Now.AddSeconds(2));
    }

    [Test]
    public async Task WriteAsync_WhenResourceCapacityIsExceeded_DropsOldestResource()
    {
        var context = new OpenTelemetryStoreTestContext(new OpenTelemetryDiagnosticsOptions { ResourceCapacity = 2 });

        await context.WriteAsync(new OpenTelemetryBatch(
            [
                context.Resource("resource-a", "api", context.Now.AddSeconds(1)),
                context.Resource("resource-b", "worker", context.Now.AddSeconds(2)),
                context.Resource("resource-c", "jobs", context.Now.AddSeconds(3))
            ],
            [], [], [], [], []));

        var result = await context.Store.QueryResourcesAsync(new OpenTelemetryResourceFilter { Take = 10 });

        await Assert.That(result.Items.Select(x => x.Id)).IsEquivalentTo(["resource-c", "resource-b"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(result.DroppedCount).IsEqualTo(1);
    }

    [Test]
    public async Task Constructor_WhenCapacitiesAreMisconfigured_ClampsToMinimumCapacity()
    {
        var context = new OpenTelemetryStoreTestContext(new OpenTelemetryDiagnosticsOptions
        {
            TraceCapacity = 0,
            SpanCapacity = 0,
            MetricPointCapacity = 0,
            LogRecordCapacity = 0
        });
        var resource = context.Resource("resource-a", "api");

        await context.WriteAsync(new OpenTelemetryBatch(
            [resource],
            [context.Trace("trace-1", resource.Id)],
            [context.Span("span-1", "trace-1", "span-1", resource.Id)],
            [context.Instrument("instrument-1", resource.Id, "metric")],
            [context.Point("point-1", "instrument-1", resource.Id)],
            [context.Log("log-1", resource.Id, "trace-1")]));

        var diagnostics = await context.Store.GetDiagnosticsAsync();

        await Assert.That(diagnostics.TraceCount).IsEqualTo(1);
        await Assert.That(diagnostics.SpanCount).IsEqualTo(1);
        await Assert.That(diagnostics.MetricPointCount).IsEqualTo(1);
        await Assert.That(diagnostics.LogRecordCount).IsEqualTo(1);
        await Assert.That(diagnostics.TraceCapacity).IsEqualTo(1);
        await Assert.That(diagnostics.SpanCapacity).IsEqualTo(1);
        await Assert.That(diagnostics.MetricPointCapacity).IsEqualTo(1);
        await Assert.That(diagnostics.LogRecordCapacity).IsEqualTo(1);
    }
}
