using Elsa.Diagnostics.Models;
using Elsa.Diagnostics.Options;
using Elsa.Diagnostics.Providers.InMemory;
using Elsa.Diagnostics.Services;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Elsa.Diagnostics.UnitTests.InMemory;

public class InMemoryServerLogProviderSourceTests
{
    private readonly InMemoryServerLogProvider _provider;

    public InMemoryServerLogProviderSourceTests()
    {
        var options = MicrosoftOptions.Create(new ServerLogStreamingOptions
        {
            RecentLogCapacity = 10,
            MaxRecentLogQuerySize = 10,
            SubscriberChannelCapacity = 4
        });
        _provider = new(options, new ServerLogSourceRegistry(options));
    }

    [Fact]
    public async Task GetRecentAsync_WhenNoSourceFilterIsApplied_ReturnsMergedSources()
    {
        await _provider.PublishAsync(CreateLog(1, "pod-a"));
        await _provider.PublishAsync(CreateLog(2, "pod-b"));

        var result = await _provider.GetRecentAsync(new());

        Assert.Equal(["pod-a", "pod-b"], result.Items.Select(x => x.SourceId));
    }

    [Fact]
    public async Task GetRecentAsync_WhenSourceFilterIsApplied_ReturnsOnlyThatSource()
    {
        await _provider.PublishAsync(CreateLog(1, "pod-a"));
        await _provider.PublishAsync(CreateLog(2, "pod-b"));
        await _provider.PublishAsync(CreateLog(3, "pod-b"));

        var result = await _provider.GetRecentAsync(new()
        {
            SourceId = "pod-b"
        });

        Assert.Equal([2, 3], result.Items.Select(x => x.Sequence));
    }

    [Fact]
    public async Task GetRecentAsync_WhenTimestampsTie_OrdersDeterministicallyAcrossSources()
    {
        var timestamp = DateTimeOffset.UtcNow;
        await _provider.PublishAsync(CreateLog(2, "pod-b", timestamp));
        await _provider.PublishAsync(CreateLog(1, "pod-a", timestamp));

        var result = await _provider.GetRecentAsync(new());

        Assert.Equal(["pod-a", "pod-b"], result.Items.Select(x => x.SourceId));
    }

    private static ServerLogEvent CreateLog(long sequence, string sourceId, DateTimeOffset? timestamp = null)
    {
        var logTimestamp = timestamp ?? DateTimeOffset.UtcNow.AddSeconds(sequence);
        return new()
        {
            Id = $"{sourceId}-{sequence}",
            Sequence = sequence,
            Timestamp = logTimestamp,
            ReceivedAt = logTimestamp,
            Level = ServerLogLevel.Information,
            Category = "Elsa",
            Message = $"Message {sequence}",
            SourceId = sourceId
        };
    }
}
