using Elsa.Studio.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Contracts;

/// <summary>
/// Observes live OpenTelemetry diagnostics updates from the active backend.
/// </summary>
public interface IOpenTelemetryObserver : IAsyncDisposable
{
    IAsyncEnumerable<OpenTelemetryStreamItem> ObserveAsync(OpenTelemetryTraceFilter filter, CancellationToken cancellationToken = default);
    IAsyncEnumerable<OpenTelemetryStreamItem> ObserveMetricsAsync(OpenTelemetryMetricFilter filter, CancellationToken cancellationToken = default);
}
