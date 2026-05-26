using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Elsa.Diagnostics.OpenTelemetry.Providers.InMemory;
using Elsa.Diagnostics.OpenTelemetry.Services;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Elsa.Diagnostics.OpenTelemetry.UnitTests.Providers;

public class OpenTelemetryStoreTestContext
{
    public OpenTelemetryStoreTestContext(OpenTelemetryDiagnosticsOptions? options = null)
    {
        Options = options ?? new OpenTelemetryDiagnosticsOptions();
        SourceRegistry = new OpenTelemetrySourceRegistry();
        Store = new InMemoryOpenTelemetryStore(OptionsFactory.Create(Options), SourceRegistry);
    }

    public OpenTelemetryDiagnosticsOptions Options { get; }
    public OpenTelemetrySourceRegistry SourceRegistry { get; }
    public InMemoryOpenTelemetryStore Store { get; }
    public DateTimeOffset Now { get; } = new(2026, 5, 26, 10, 0, 0, TimeSpan.Zero);

    public async Task WriteAsync(OpenTelemetryBatch batch)
    {
        await Store.WriteAsync(batch);
    }

    public TelemetryResource Resource(string id, string serviceName, DateTimeOffset? lastSeen = null, TelemetryResourceStatus status = TelemetryResourceStatus.Active)
    {
        return new(id, serviceName, $"{id}-instance", "dotnet", new Dictionary<string, string?>(), lastSeen ?? Now, status);
    }

    public TelemetryTrace Trace(string traceId, string resourceId, DateTimeOffset? startTime = null, SpanStatus status = SpanStatus.Ok, params string[] workflowInstanceIds)
    {
        var start = startTime ?? Now;
        return new(traceId, $"{traceId}-root", $"trace-{traceId}", start, start.AddMilliseconds(25), TimeSpan.FromMilliseconds(25), status, [resourceId], workflowInstanceIds, 2);
    }

    public TelemetrySpan Span(string id, string traceId, string spanId, string resourceId, DateTimeOffset? startTime = null)
    {
        var start = startTime ?? Now;
        return new(id, traceId, spanId, null, resourceId, $"span-{spanId}", "internal", start, start.AddMilliseconds(10), SpanStatus.Ok, null, new Dictionary<string, string?>(), [], []);
    }

    public MetricInstrument Instrument(string id, string resourceId, string name)
    {
        return new(id, resourceId, name, "ms", null, MetricKind.Gauge, new Dictionary<string, string?>());
    }

    public MetricPoint Point(string id, string instrumentId, string resourceId, DateTimeOffset? timestamp = null)
    {
        return new(id, instrumentId, resourceId, timestamp ?? Now, 42, null, null, new Dictionary<string, string?>(), null, null);
    }

    public OtlpLogRecord Log(string id, string resourceId, string traceId, string severity = "Information", string body = "message")
    {
        return new(id, resourceId, Now, severity, null, body, traceId, $"{traceId}-span", new Dictionary<string, string?>());
    }
}
