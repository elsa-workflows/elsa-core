using Elsa.Studio.Diagnostics.StructuredLogs.Models;

namespace Elsa.Studio.Diagnostics.StructuredLogs.Contracts;

/// <summary>
/// Observes live structured log events from the active backend.
/// </summary>
public interface IStructuredLogObserver : IAsyncDisposable
{
    event Func<StructuredLogEvent, Task>? LogReceived;
    event Func<StructuredLogDroppedEventSummary, Task>? DroppedEventsReceived;
    event Func<StructuredLogSource, Task>? SourceChanged;
    event Func<StructuredLogConnectionStatus, Task>? ConnectionStatusChanged;

    Task StartAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default);
    Task UpdateFilterAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default);
    Task ReconnectAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default);
}
