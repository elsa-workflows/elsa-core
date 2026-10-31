using System.Runtime.CompilerServices;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Services;

namespace Elsa.Diagnostics.StructuredLogs.UnitTests;

public class DefaultStructuredLogProviderTests
{
    private readonly List<string> _calls = new();
    private readonly CapturingStructuredLogStore _store;
    private readonly CapturingStructuredLogLiveFeed _liveFeed;
    private readonly DefaultStructuredLogProvider _provider;

    public DefaultStructuredLogProviderTests()
    {
        _store = new(_calls);
        _liveFeed = new(_calls);
        _provider = new(_store, _liveFeed);
    }

    [Fact]
    public async Task PublishAsync_WritesToStoreBeforePublishingToLiveFeed()
    {
        var logEvent = CreateLogEvent(1);

        await _provider.PublishAsync(logEvent);

        Assert.Equal(["store", "live-feed"], _calls);
        Assert.Same(logEvent, Assert.Single(_store.WrittenEvents));
        Assert.Same(logEvent, Assert.Single(_liveFeed.PublishedEvents));
    }

    [Fact]
    public async Task GetRecentAsync_DelegatesToStore()
    {
        var filter = new StructuredLogFilter { Take = 10 };
        var expected = new RecentStructuredLogsResult([CreateLogEvent(1)], 2);
        _store.RecentResult = expected;

        var result = await _provider.GetRecentAsync(filter);

        Assert.Same(filter, _store.LastQueryFilter);
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task SubscribeAsync_YieldsOnlyLogEventsFromLiveFeed()
    {
        var first = CreateLogEvent(1);
        var second = CreateLogEvent(2);
        _liveFeed.StreamItems.Add(StructuredLogStreamItem.FromLogEvent(first));
        _liveFeed.StreamItems.Add(StructuredLogStreamItem.FromDroppedEvents(new("source-a", 3, "SubscriberChannelFull")));
        _liveFeed.StreamItems.Add(StructuredLogStreamItem.FromLogEvent(second));

        var result = new List<StructuredLogEvent>();
        await foreach (var logEvent in _provider.SubscribeAsync(new()))
            result.Add(logEvent);

        Assert.Equal([first, second], result);
    }

    [Fact]
    public async Task SubscribeWithDroppedEventsAsync_DelegatesToLiveFeed()
    {
        var filter = new StructuredLogFilter { SourceId = "source-a" };
        var expected = StructuredLogStreamItem.FromDroppedEvents(new("source-a", 1, "SubscriberChannelFull"));
        _liveFeed.StreamItems.Add(expected);

        var result = new List<StructuredLogStreamItem>();
        await foreach (var item in _provider.SubscribeWithDroppedEventsAsync(filter))
            result.Add(item);

        Assert.Same(filter, _liveFeed.LastSubscribeFilter);
        Assert.Equal([expected], result);
    }

    [Fact]
    public async Task ListSourcesAsync_DelegatesToStore()
    {
        var expected = new List<StructuredLogSource>
        {
            new() { Id = "source-a", DisplayName = "Source A" }
        };
        _store.Sources = expected;

        var result = await _provider.ListSourcesAsync();

        Assert.Same(expected, result);
    }

    private static StructuredLogEvent CreateLogEvent(long sequence) =>
        new()
        {
            Sequence = sequence,
            Timestamp = DateTimeOffset.UtcNow,
            ReceivedAt = DateTimeOffset.UtcNow,
            Level = StructuredLogLevel.Information,
            Category = "Elsa.Workflows",
            Message = $"Message {sequence}",
            SourceId = "source-a"
        };

    private sealed class CapturingStructuredLogStore(List<string> calls) : IStructuredLogStore
    {
        public List<StructuredLogEvent> WrittenEvents { get; } = new();
        public StructuredLogFilter? LastQueryFilter { get; private set; }
        public RecentStructuredLogsResult RecentResult { get; set; } = new([], 0);
        public IReadOnlyCollection<StructuredLogSource> Sources { get; set; } = [];

        public ValueTask WriteAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default)
        {
            calls.Add("store");
            WrittenEvents.Add(logEvent);
            return ValueTask.CompletedTask;
        }

        public ValueTask<RecentStructuredLogsResult> QueryAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default)
        {
            LastQueryFilter = filter;
            return ValueTask.FromResult(RecentResult);
        }

        public ValueTask<IReadOnlyCollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(Sources);
        }
    }

    private sealed class CapturingStructuredLogLiveFeed(List<string> calls) : IStructuredLogLiveFeed
    {
        public List<StructuredLogEvent> PublishedEvents { get; } = new();
        public List<StructuredLogStreamItem> StreamItems { get; } = new();
        public StructuredLogFilter? LastSubscribeFilter { get; private set; }

        public ValueTask PublishAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default)
        {
            calls.Add("live-feed");
            PublishedEvents.Add(logEvent);
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<StructuredLogStreamItem> SubscribeAsync(
            StructuredLogFilter filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            LastSubscribeFilter = filter;

            foreach (var item in StreamItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
                await Task.Yield();
            }
        }
    }
}
