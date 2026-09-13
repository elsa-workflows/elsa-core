using Elsa.Diagnostics.OpenTelemetry.Models;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.OpenTelemetry.UnitTests.Providers;

public class OpenTelemetryTraceQueryTests
{
    [Test]
    public async Task QueryTracesAsync_WhenFilteringByServiceName_ReturnsMatchingResourceTraces()
    {
        var context = new OpenTelemetryStoreTestContext();
        var api = context.Resource("resource-api", "api");
        var worker = context.Resource("resource-worker", "worker");
        await context.WriteAsync(new OpenTelemetryBatch([api, worker], [context.Trace("trace-api", api.Id), context.Trace("trace-worker", worker.Id)], [], [], [], []));

        var result = await context.Store.QueryTracesAsync(new OpenTelemetryTraceFilter { ServiceName = "api" });

        var trace = await Assert.That(result.Items).HasSingleItem();
        await Assert.That(trace.TraceId).IsEqualTo("trace-api");
    }

    [Test]
    public async Task GetTraceAsync_ReturnsOrderedSpansResourcesAndTraceLogs()
    {
        var context = new OpenTelemetryStoreTestContext();
        var resource = context.Resource("resource-api", "api");
        var trace = context.Trace("trace-1", resource.Id);
        var laterSpan = context.Span("span-record-2", trace.TraceId, "span-2", resource.Id, context.Now.AddSeconds(2));
        var earlierSpan = context.Span("span-record-1", trace.TraceId, "span-1", resource.Id, context.Now.AddSeconds(1));
        var log = context.Log("log-1", resource.Id, trace.TraceId);
        await context.WriteAsync(new OpenTelemetryBatch([resource], [trace], [laterSpan, earlierSpan], [], [], [log]));

        var detail = await context.Store.GetTraceAsync(trace.TraceId);

        await Assert.That(detail).IsNotNull();
        await Assert.That(detail.Spans.Select(x => x.SpanId)).IsEquivalentTo(["span-1", "span-2"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        var detailResource = await Assert.That(detail.Resources).HasSingleItem();
        var detailLog = await Assert.That(detail.Logs).HasSingleItem();
        await Assert.That(detailResource.Id).IsEqualTo(resource.Id);
        await Assert.That(detailLog.Id).IsEqualTo(log.Id);
    }
}
