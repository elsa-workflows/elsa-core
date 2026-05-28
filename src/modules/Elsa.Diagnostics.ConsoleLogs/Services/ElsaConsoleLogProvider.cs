using ConsoleLogStreaming.Core;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

internal sealed class ElsaConsoleLogProvider(IConsoleLogProvider inner, IConsoleLogContextAccessor contextAccessor) : IConsoleLogProvider
{
    public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
    {
        return inner.PublishAsync(Enrich(line), cancellationToken);
    }

    public ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
    {
        return inner.GetRecentAsync(filter, cancellationToken);
    }

    public IAsyncEnumerable<ConsoleLogStreamingItem> SubscribeAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
    {
        return inner.SubscribeAsync(filter, cancellationToken);
    }

    public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        return inner.ListSourcesAsync(cancellationToken);
    }

    private ConsoleLogLine Enrich(ConsoleLogLine line)
    {
        var ambientMetadata = contextAccessor.GetMetadata();
        if (ambientMetadata.Count == 0)
            return line;

        var metadata = new Dictionary<string, string>(line.Metadata, StringComparer.OrdinalIgnoreCase);
        foreach (var item in ambientMetadata)
            metadata[item.Key] = item.Value;

        return line with { Metadata = metadata };
    }
}
