using Elsa.Studio.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Contracts;

/// <summary>
/// Loads OpenTelemetry diagnostics data from the active backend.
/// </summary>
public interface IOpenTelemetryService
{
    Task<OpenTelemetryResourceResult> GetResourcesAsync(OpenTelemetryResourceFilter filter, CancellationToken cancellationToken = default);
    Task<OpenTelemetryTraceResult> GetTracesAsync(OpenTelemetryTraceFilter filter, CancellationToken cancellationToken = default);
    Task<OpenTelemetryTraceDetail?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default);
    Task<OpenTelemetryMetricResult> GetMetricsAsync(OpenTelemetryMetricFilter filter, CancellationToken cancellationToken = default);
    Task<OpenTelemetryLogResult> GetLogsAsync(OpenTelemetryLogFilter filter, CancellationToken cancellationToken = default);
    Task<OpenTelemetryStorageDiagnostics> GetStorageDiagnosticsAsync(CancellationToken cancellationToken = default);
    Task<CollectorConfiguration?> GetCollectorConfigurationAsync(CancellationToken cancellationToken = default);
}
