using Elsa.Diagnostics.ConsoleLogs.Providers.InMemory;
using Elsa.Diagnostics.ConsoleLogs.Services;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests.InMemory;

public class InMemoryConsoleLogProviderTests
{
    private readonly ConsoleLogSource _source = new() { Id = "local", DisplayName = "local", MachineName = "machine", Health = ConsoleLogSourceHealth.Connected };
    private readonly InMemoryConsoleLogProvider _provider;

    public InMemoryConsoleLogProviderTests()
    {
        _provider = new(
            Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions { RecentLogCapacity = 2, MaxRecentQuerySize = 1, SubscriberChannelCapacity = 10 }),
            new ConsoleLogSourceRegistry(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions())));
    }

    [Fact]
    public async Task GetRecentAsync_ClampsLimitAndOrdersByReceivedAt()
    {
        await _provider.PublishAsync(Line("one", 1));
        await _provider.PublishAsync(Line("two", 2));

        var result = await _provider.GetRecentAsync(new ConsoleLogFilter { Limit = 10 });

        var line = Assert.Single(result.Items);
        Assert.Equal("two", line.Text);
    }

    [Fact]
    public async Task SubscribeAsync_StreamsMatchingLines()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var readTask = ReadOneAsync(_provider.SubscribeAsync(new ConsoleLogFilter { Stream = ConsoleLogStream.Stderr }, cancellation.Token), cancellation.Token);

        await _provider.PublishAsync(Line("ignored", 1));
        await _provider.PublishAsync(Line("error", 2, ConsoleLogStream.Stderr));

        var item = await readTask;
        Assert.Equal("error", item.Line?.Text);
    }

    private ConsoleLogLine Line(string text, long sequence, ConsoleLogStream stream = ConsoleLogStream.Stdout) => new()
    {
        Text = text,
        Sequence = sequence,
        Stream = stream,
        Source = _source,
        Timestamp = DateTimeOffset.UnixEpoch.AddSeconds(sequence),
        ReceivedAt = DateTimeOffset.UnixEpoch.AddSeconds(sequence)
    };

    private static async Task<ConsoleLogStreamItem> ReadOneAsync(IAsyncEnumerable<ConsoleLogStreamItem> items, CancellationToken cancellationToken)
    {
        await foreach (var item in items.WithCancellation(cancellationToken))
            return item;

        throw new InvalidOperationException("No item was streamed.");
    }
}
