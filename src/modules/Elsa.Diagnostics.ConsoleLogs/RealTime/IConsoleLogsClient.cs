namespace Elsa.Diagnostics.ConsoleLogs.RealTime;

public interface IConsoleLogsClient
{
    Task ReceiveConsoleLogLineAsync(ConsoleLogLine line, CancellationToken cancellationToken = default);

    Task ReceiveDroppedLinesAsync(ConsoleLogDroppedSummary summary, CancellationToken cancellationToken = default);

    Task ReceiveSourceChangedAsync(ConsoleLogSource source, CancellationToken cancellationToken = default);
}
