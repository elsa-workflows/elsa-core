using Elsa.Studio.Diagnostics.StructuredLogs.Models;
using Refit;

namespace Elsa.Studio.Diagnostics.StructuredLogs.Client;

/// <summary>
/// Backend API for structured log diagnostics.
/// </summary>
public interface IStructuredLogsApi
{
    /// <summary>
    /// Gets recent log events.
    /// </summary>
    [Post("/diagnostics/structured-logs/recent")]
    Task<RecentStructuredLogsResult> GetRecentAsync([Body] StructuredLogFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists structured log sources.
    /// </summary>
    [Get("/diagnostics/structured-logs/sources")]
    Task<ICollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets storage-level diagnostics.
    /// </summary>
    [Get("/diagnostics/structured-logs/storage")]
    Task<StructuredLogStorageDiagnostics> GetStorageDiagnosticsAsync(CancellationToken cancellationToken = default);
}
