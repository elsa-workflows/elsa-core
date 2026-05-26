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
}
