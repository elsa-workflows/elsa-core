using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests;

public class SqliteStructuredLogSourceTests
{
    [Fact]
    public async Task QueryAsync_WhenTimestampsTie_OrdersDeterministicallyAcrossSources()
    {
        await using var host = new SqliteStructuredLogTestHost();
        var timestamp = new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero);
        await host.WriteAsync(
            CreateLog("zzz", 1, "pod-b", timestamp),
            CreateLog("aaa", 2, "pod-a", timestamp));

        var result = await host.Store.QueryAsync(new() { Take = 10 });

        Assert.Equal(["pod-a", "pod-b"], result.Items.Select(x => x.SourceId));
    }

    [Fact]
    public async Task ListSourcesAsync_PrefersRegistryMetadataAndHeartbeat()
    {
        await using var host = new SqliteStructuredLogTestHost(configureStructuredLogs: options =>
            options.SourceHeartbeatTimeout = TimeSpan.FromSeconds(5));
        var current = host.SourceRegistry.Current;
        await host.WriteAsync(CreateLog("local-1", 1, current.Id, DateTimeOffset.UtcNow));
        host.SourceRegistry.MarkSeen("stale-pod", DateTimeOffset.UtcNow.AddMinutes(-1));

        var sources = await host.Store.ListSourcesAsync();
        var currentSource = Assert.Single(sources, x => x.Id == current.Id);
        var staleSource = Assert.Single(sources, x => x.Id == "stale-pod");

        Assert.Equal(current.DisplayName, currentSource.DisplayName);
        Assert.Equal(current.ServiceName, currentSource.ServiceName);
        Assert.Equal(current.ProcessId, currentSource.ProcessId);
        Assert.NotEqual(0, currentSource.ProcessId);
        Assert.Equal(StructuredLogSourceStatus.Connected, currentSource.Status);
        Assert.Equal(StructuredLogSourceStatus.Stale, staleSource.Status);
    }

    [Fact]
    public async Task WriteAsync_UpdatesRegistryBeforeFlush()
    {
        await using var host = new SqliteStructuredLogTestHost();
        var receivedAt = DateTimeOffset.UtcNow;

        await host.Buffer.WriteAsync(CreateLog("buffered-1", 1, "buffered-pod", receivedAt));

        var source = Assert.Single(await host.Store.ListSourcesAsync(), x => x.Id == "buffered-pod");
        Assert.Equal(receivedAt, source.LastSeen);
        Assert.Equal(StructuredLogSourceStatus.Connected, source.Status);
        Assert.Empty((await host.Store.QueryAsync(new() { Take = 10 })).Items);
    }

    [Fact]
    public async Task ListSourcesAsync_UsesPersistedSourcesOnlyAsFallback()
    {
        await using var firstHost = new SqliteStructuredLogTestHost();
        await firstHost.WriteAsync(CreateLog("remote-1", 1, "remote-pod", DateTimeOffset.UtcNow));
        var connectionString = firstHost.ConnectionString;

        await using var secondHost = new SqliteStructuredLogTestHost(options => options.ConnectionString = connectionString);
        var current = secondHost.SourceRegistry.Current;
        var sources = await secondHost.Store.ListSourcesAsync();
        var remote = Assert.Single(sources, x => x.Id == "remote-pod");
        var local = Assert.Single(sources, x => x.Id == current.Id);

        Assert.Equal("remote-pod", remote.DisplayName);
        Assert.Equal(0, remote.ProcessId);
        Assert.Equal(StructuredLogSourceStatus.Connected, remote.Status);
        Assert.Equal(current.DisplayName, local.DisplayName);
        Assert.Equal(current.ProcessId, local.ProcessId);
        Assert.NotEqual(0, local.ProcessId);
    }

    [Fact]
    public async Task StoreWriteManyAsync_WhenFlushIsOlder_DoesNotRegressLastSeen()
    {
        await using var host = new SqliteStructuredLogTestHost();
        var newer = DateTimeOffset.UtcNow;
        var older = newer.AddMinutes(-1);
        var store = host.Services.GetRequiredService<RelationalStructuredLogStore>();

        await host.Buffer.WriteAsync(CreateLog("newer-1", 2, "pod-b", newer));
        await store.WriteManyAsync([CreateLog("older-1", 1, "pod-b", older)]);

        var source = Assert.Single(host.SourceRegistry.List(), x => x.Id == "pod-b");
        Assert.Equal(newer, source.LastSeen);
        Assert.Equal(StructuredLogSourceStatus.Connected, source.Status);
    }

    [Fact]
    public async Task ListSourcesAsync_WhenRegistryHasCaseDistinctIds_KeepsBoth()
    {
        await using var host = new SqliteStructuredLogTestHost();
        var seenAt = DateTimeOffset.UtcNow;
        host.SourceRegistry.MarkSeen("pod-a", seenAt);
        host.SourceRegistry.MarkSeen("POD-A", seenAt);

        var sources = await host.Store.ListSourcesAsync();

        Assert.Contains(sources, x => x.Id == "pod-a");
        Assert.Contains(sources, x => x.Id == "POD-A");
        Assert.Equal(2, sources.Count(x => x.Id.Equals("pod-a", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task StoreWriteManyAsync_MarksRegistrySeen()
    {
        await using var host = new SqliteStructuredLogTestHost();
        var store = host.Services.GetRequiredService<RelationalStructuredLogStore>();
        var receivedAt = DateTimeOffset.UtcNow;

        await store.WriteManyAsync([CreateLog("direct-1", 1, "direct-pod", receivedAt)]);

        var source = Assert.Single(host.SourceRegistry.List(), x => x.Id == "direct-pod");
        Assert.Equal(receivedAt, source.LastSeen);
        Assert.Equal(StructuredLogSourceStatus.Connected, source.Status);
    }

    private static StructuredLogEvent CreateLog(string id, long sequence, string sourceId, DateTimeOffset timestamp)
    {
        return new()
        {
            Id = id,
            Sequence = sequence,
            Timestamp = timestamp,
            ReceivedAt = timestamp,
            Level = StructuredLogLevel.Information,
            Category = "Elsa.Tests",
            Message = $"Message {sequence}",
            SourceId = sourceId
        };
    }
}
