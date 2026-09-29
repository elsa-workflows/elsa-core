using Elsa.Studio.Diagnostics.ConsoleLogs.Models;
using Refit;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Client;

/// <summary>
/// Diagnostics console logs backend API.
/// </summary>
public interface IConsoleLogsApi
{
    /// <summary>
    /// Gets recent console lines.
    /// </summary>
    [Post("/diagnostics/console-logs/recent")]
    Task<RecentConsoleLinesResult> GetRecentAsync([Body] ConsoleLogFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists known console sources.
    /// </summary>
    [Get("/diagnostics/console-logs/sources")]
    Task<ICollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default);
}
