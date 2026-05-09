using Elsa.ServerLogs.Models;
using Elsa.ServerLogs.Options;
using Elsa.ServerLogs.Providers.InMemory;
using Elsa.ServerLogs.Services;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Elsa.ServerLogs.UnitTests.InMemory;

public class InMemoryServerLogProviderTests
{
    private readonly ServerLogStreamingOptions _options = new()
    {
        RecentLogCapacity = 3,
        MaxRecentLogQuerySize = 2,
        SubscriberChannelCapacity = 4
    };

    private readonly InMemoryServerLogProvider _provider;

    public InMemoryServerLogProviderTests()
    {
        var options = MicrosoftOptions.Create(_options);
        var sources = new ServerLogSourceRegistry(options);
        _provider = new(options, sources);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsNewestMatchingEventsWithinQueryLimit()
    {
        await _provider.PublishAsync(CreateLog(1, ServerLogLevel.Information, "Elsa.Workflows"));
        await _provider.PublishAsync(CreateLog(2, ServerLogLevel.Warning, "Elsa.Workflows"));
        await _provider.PublishAsync(CreateLog(3, ServerLogLevel.Error, "Elsa.Http"));
        await _provider.PublishAsync(CreateLog(4, ServerLogLevel.Critical, "Elsa.Workflows"));

        var result = await _provider.GetRecentAsync(new()
        {
            CategoryPrefix = "Elsa.Workflows",
            MinimumLevel = ServerLogLevel.Warning,
            Take = 10
        });

        Assert.Equal(1, result.DroppedEvents);
        Assert.Equal([2, 4], result.Items.Select(x => x.Sequence));
    }

    [Fact]
    public async Task SubscribeAsync_YieldsOnlyMatchingLiveEvents()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var subscription = _provider.SubscribeAsync(new()
        {
            SourceId = "source-b"
        }, cancellationTokenSource.Token).GetAsyncEnumerator();

        var next = subscription.MoveNextAsync().AsTask();

        await _provider.PublishAsync(CreateLog(1, ServerLogLevel.Information, sourceId: "source-a"));
        await _provider.PublishAsync(CreateLog(2, ServerLogLevel.Information, sourceId: "source-b"));

        Assert.True(await next.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(2, subscription.Current.Sequence);
    }

    [Fact]
    public async Task SubscribeWithDroppedEventsAsync_WhenSubscriberCapacityIsReached_YieldsDroppedSummary()
    {
        _options.SubscriberChannelCapacity = 1;
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var subscription = _provider.SubscribeWithDroppedEventsAsync(new(), cancellationTokenSource.Token).GetAsyncEnumerator();
        var next = subscription.MoveNextAsync().AsTask();

        await _provider.PublishAsync(CreateLog(1, ServerLogLevel.Information));
        await _provider.PublishAsync(CreateLog(2, ServerLogLevel.Information));

        Assert.True(await next.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(1, subscription.Current.LogEvent!.Sequence);

        Assert.True(await subscription.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(1, subscription.Current.DroppedEvents!.DroppedCount);
        Assert.Equal("SubscriberChannelFull", subscription.Current.DroppedEvents.Reason);
    }

    private static ServerLogEvent CreateLog(long sequence, ServerLogLevel level, string category = "Elsa", string sourceId = "source-a") =>
        new()
        {
            Sequence = sequence,
            Timestamp = DateTimeOffset.UtcNow.AddSeconds(sequence),
            ReceivedAt = DateTimeOffset.UtcNow.AddSeconds(sequence),
            Level = level,
            Category = category,
            Message = $"Message {sequence}",
            SourceId = sourceId
        };
}
