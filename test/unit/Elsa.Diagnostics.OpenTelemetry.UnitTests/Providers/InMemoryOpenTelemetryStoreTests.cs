using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Options;

namespace Elsa.Diagnostics.OpenTelemetry.UnitTests.Providers;

public class InMemoryOpenTelemetryStoreTests
{
    [Fact]
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

        Assert.Equal(["trace-2", "trace-3"], result.Items.Select(x => x.TraceId));
        Assert.Equal(1, result.DroppedCount);
        Assert.Equal(1, diagnostics.DroppedTraceCount);
        Assert.Equal(2, diagnostics.TraceCount);
    }

    [Fact]
    public async Task QueryResourcesAsync_WhenTakeExceedsMaxQuerySize_ClampsResult()
    {
        var context = new OpenTelemetryStoreTestContext(new OpenTelemetryDiagnosticsOptions { MaxQuerySize = 1 });

        await context.WriteAsync(new OpenTelemetryBatch([context.Resource("resource-a", "api"), context.Resource("resource-b", "worker")], [], [], [], [], []));

        var result = await context.Store.QueryResourcesAsync(new OpenTelemetryResourceFilter { Take = 10 });

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task QueryTracesAsync_WhenTraceIdAppearsInMultipleBatches_ReturnsLatestTrace()
    {
        var context = new OpenTelemetryStoreTestContext(new OpenTelemetryDiagnosticsOptions { TraceCapacity = 10 });
        var resource = context.Resource("resource-a", "api");

        await context.WriteAsync(new OpenTelemetryBatch([resource], [context.Trace("trace-1", resource.Id, context.Now.AddSeconds(1))], [], [], [], []));
        await context.WriteAsync(new OpenTelemetryBatch([], [context.Trace("trace-1", resource.Id, context.Now.AddSeconds(2))], [], [], [], []));

        var result = await context.Store.QueryTracesAsync(new OpenTelemetryTraceFilter { Take = 10 });

        var trace = Assert.Single(result.Items);
        Assert.Equal(context.Now.AddSeconds(2), trace.StartTime);
    }

    [Fact]
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

        Assert.Equal(["resource-c", "resource-b"], result.Items.Select(x => x.Id));
        Assert.Equal(1, result.DroppedCount);
    }

    [Fact]
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

        Assert.Equal(1, diagnostics.TraceCount);
        Assert.Equal(1, diagnostics.SpanCount);
        Assert.Equal(1, diagnostics.MetricPointCount);
        Assert.Equal(1, diagnostics.LogRecordCount);
        Assert.Equal(1, diagnostics.TraceCapacity);
        Assert.Equal(1, diagnostics.SpanCapacity);
        Assert.Equal(1, diagnostics.MetricPointCapacity);
        Assert.Equal(1, diagnostics.LogRecordCapacity);
    }
}
