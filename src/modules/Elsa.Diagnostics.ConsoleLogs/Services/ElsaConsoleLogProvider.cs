using ConsoleLogStreaming.Core;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

internal sealed class ElsaConsoleLogProvider(IConsoleLogProvider inner, IConsoleLogContextAccessor contextAccessor) : IConsoleLogProvider
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadataFilter = new Dictionary<string, string>();

    public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
    {
        return inner.PublishAsync(Enrich(line), cancellationToken);
    }

    public async ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
    {
        if (!HasMetadataFilter(filter))
            return await inner.GetRecentAsync(filter, cancellationToken);

        var requestedLimit = filter.Limit;
        var result = await inner.GetRecentAsync(filter with
        {
            Metadata = EmptyMetadataFilter,
            Limit = null
        }, cancellationToken);
        var items = result.Items.Where(x => MatchesMetadata(x, filter.Metadata)).ToArray();

        if (requestedLimit is > 0)
            items = items.TakeLast(requestedLimit.Value).ToArray();

        return result with { Items = items };
    }

    public async IAsyncEnumerable<ConsoleLogStreamingItem> SubscribeAsync(
        ConsoleLogFilter filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!HasMetadataFilter(filter))
        {
            await foreach (var item in inner.SubscribeAsync(filter, cancellationToken).ConfigureAwait(false))
                yield return item;

            yield break;
        }

        await foreach (var item in inner.SubscribeAsync(filter with { Metadata = EmptyMetadataFilter }, cancellationToken).ConfigureAwait(false))
        {
            if (item.Line != null && !MatchesMetadata(item.Line, filter.Metadata))
                continue;

            yield return item;
        }
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

    private static bool HasMetadataFilter(ConsoleLogFilter filter) => filter.Metadata.Count > 0;

    private static bool MatchesMetadata(ConsoleLogLine line, IReadOnlyDictionary<string, string> metadata)
    {
        foreach (var (key, value) in metadata)
        {
            if (!line.Metadata.TryGetValue(key, out var candidate) || !string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}
