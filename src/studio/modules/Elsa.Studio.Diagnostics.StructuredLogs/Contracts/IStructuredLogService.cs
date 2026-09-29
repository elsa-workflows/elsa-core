using Elsa.Studio.Diagnostics.StructuredLogs.Models;

namespace Elsa.Studio.Diagnostics.StructuredLogs.Contracts;

/// <summary>
/// Loads structured log data from the active backend.
/// </summary>
public interface IStructuredLogService
{
    /// <summary>
    /// Gets recent structured log events.
    /// </summary>
    Task<RecentStructuredLogsResult> GetRecentAsync(StructuredLogFilter filter, int rowCap, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists structured log sources.
    /// </summary>
    Task<ICollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets storage-level diagnostics.
    /// </summary>
    Task<StructuredLogStorageDiagnostics> GetStorageDiagnosticsAsync(CancellationToken cancellationToken = default);
}
