using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Options;
using Elsa.Diagnostics.StructuredLogs.Providers.InMemory;
using Elsa.Diagnostics.StructuredLogs.Services;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Elsa.Diagnostics.StructuredLogs.UnitTests.InMemory;

public class InMemoryStructuredLogProviderTests
{
    private readonly StructuredLogsOptions _options = new()
    {
        RecentLogCapacity = 3,
        MaxRecentLogQuerySize = 2,
        SubscriberChannelCapacity = 4
    };

    private readonly InMemoryStructuredLogProvider _provider;

    public InMemoryStructuredLogProviderTests()
    {
        var options = MicrosoftOptions.Create(_options);
        var sources = new StructuredLogSourceRegistry(options);
        _provider = new(options, sources);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsNewestMatchingEventsWithinQueryLimit()
    {
        await _provider.PublishAsync(CreateLog(1, StructuredLogLevel.Information, "Elsa.Workflows"));
        await _provider.PublishAsync(CreateLog(2, StructuredLogLevel.Warning, "Elsa.Workflows"));
        await _provider.PublishAsync(CreateLog(3, StructuredLogLevel.Error, "Elsa.Http"));
        await _provider.PublishAsync(CreateLog(4, StructuredLogLevel.Critical, "Elsa.Workflows"));

        var result = await _provider.GetRecentAsync(new()
        {
            CategoryPrefix = "Elsa.Workflows",
            MinimumLevel = StructuredLogLevel.Warning,
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

        await _provider.PublishAsync(CreateLog(1, StructuredLogLevel.Information, sourceId: "source-a"));
        await _provider.PublishAsync(CreateLog(2, StructuredLogLevel.Information, sourceId: "source-b"));

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

        await _provider.PublishAsync(CreateLog(1, StructuredLogLevel.Information));
        await _provider.PublishAsync(CreateLog(2, StructuredLogLevel.Information));

        Assert.True(await next.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(1, subscription.Current.LogEvent!.Sequence);

        Assert.True(await subscription.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(1, subscription.Current.DroppedEvents!.DroppedCount);
        Assert.Equal("SubscriberChannelFull", subscription.Current.DroppedEvents.Reason);
    }

    private static StructuredLogEvent CreateLog(long sequence, StructuredLogLevel level, string category = "Elsa", string sourceId = "source-a") =>
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
