using Elsa.Studio.Diagnostics.ConsoleLogs.Models;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Contracts;

/// <summary>
/// Loads diagnostics console logs through the active backend.
/// </summary>
public interface IConsoleLogService
{
    /// <summary>
    /// Gets recent console lines.
    /// </summary>
    Task<RecentConsoleLinesResult> GetRecentAsync(ConsoleLogFilter filter, int rowCap, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists known console sources.
    /// </summary>
    Task<ICollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default);
}
