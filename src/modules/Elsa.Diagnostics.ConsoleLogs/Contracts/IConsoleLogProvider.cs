namespace Elsa.Diagnostics.ConsoleLogs.Contracts;

public interface IConsoleLogProvider
{
    ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default);

    ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ConsoleLogStreamItem> SubscribeAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default);
}
