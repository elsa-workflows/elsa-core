using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Diagnostics.OpenTelemetry.Services;

public class DefaultOpenTelemetryProvider(IOpenTelemetryStore store, ICollectorConfigurationProvider collectorConfigurationProvider) : IOpenTelemetryProvider
{
    public ValueTask<OpenTelemetryResourceResult> GetResourcesAsync(OpenTelemetryResourceFilter filter, CancellationToken cancellationToken = default) => store.QueryResourcesAsync(filter, cancellationToken);

    public ValueTask<OpenTelemetryTraceResult> GetTracesAsync(OpenTelemetryTraceFilter filter, CancellationToken cancellationToken = default) => store.QueryTracesAsync(filter, cancellationToken);

    public ValueTask<OpenTelemetryTraceDetail?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default) => store.GetTraceAsync(traceId, cancellationToken);

    public ValueTask<OpenTelemetryMetricResult> GetMetricsAsync(OpenTelemetryMetricFilter filter, CancellationToken cancellationToken = default) => store.QueryMetricsAsync(filter, cancellationToken);

    public ValueTask<OpenTelemetryLogResult> GetLogsAsync(OpenTelemetryLogFilter filter, CancellationToken cancellationToken = default) => store.QueryLogsAsync(filter, cancellationToken);

    public ValueTask<OpenTelemetryStorageDiagnostics> GetStorageDiagnosticsAsync(CancellationToken cancellationToken = default) => store.GetDiagnosticsAsync(cancellationToken);

    public ValueTask<CollectorConfiguration> GetCollectorConfigurationAsync(CancellationToken cancellationToken = default) => collectorConfigurationProvider.GetAsync(cancellationToken);
}
