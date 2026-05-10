using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Options;
using Elsa.Diagnostics.StructuredLogs.Providers.InMemory;
using Elsa.Diagnostics.StructuredLogs.Services;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Elsa.Diagnostics.StructuredLogs.UnitTests.InMemory;

public class InMemoryStructuredLogProviderSourceTests
{
    private readonly InMemoryStructuredLogProvider _provider;

    public InMemoryStructuredLogProviderSourceTests()
    {
        var options = MicrosoftOptions.Create(new StructuredLogsOptions
        {
            RecentLogCapacity = 10,
            MaxRecentLogQuerySize = 10,
            SubscriberChannelCapacity = 4
        });
        _provider = new(options, new StructuredLogSourceRegistry(options));
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

    private static StructuredLogEvent CreateLog(long sequence, string sourceId, DateTimeOffset? timestamp = null)
    {
        var logTimestamp = timestamp ?? DateTimeOffset.UtcNow.AddSeconds(sequence);
        return new()
        {
            Id = $"{sourceId}-{sequence}",
            Sequence = sequence,
            Timestamp = logTimestamp,
            ReceivedAt = logTimestamp,
            Level = StructuredLogLevel.Information,
            Category = "Elsa",
            Message = $"Message {sequence}",
            SourceId = sourceId
        };
    }
}
