using Elsa.Studio.Diagnostics.ConsoleLogs.Models;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Contracts;

/// <summary>
/// Observes live diagnostics console lines.
/// </summary>
public interface IConsoleLogObserver : IAsyncDisposable
{
    /// <summary>
    /// Raised when a console line is received.
    /// </summary>
    event Func<ConsoleLogLine, Task>? LineReceived;

    /// <summary>
    /// Raised when backend dropped lines are reported.
    /// </summary>
    event Func<ConsoleLogDroppedLineSummary, Task>? DroppedLinesReceived;

    /// <summary>
    /// Raised when source metadata changes.
    /// </summary>
    event Func<ConsoleLogSource, Task>? SourceChanged;

    /// <summary>
    /// Raised when connection status changes.
    /// </summary>
    event Func<ConsoleLogConnectionStatus, Task>? ConnectionStatusChanged;

    /// <summary>
    /// Starts live streaming.
    /// </summary>
    Task StartAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the live filter.
    /// </summary>
    Task UpdateFilterAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reconnects the live stream.
    /// </summary>
    Task ReconnectAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default);
}
