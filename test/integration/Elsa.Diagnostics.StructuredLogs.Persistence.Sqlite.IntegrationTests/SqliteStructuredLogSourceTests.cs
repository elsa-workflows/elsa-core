using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests;

public class SqliteStructuredLogSourceTests
{
    [Test]
    public async Task QueryAsync_WhenTimestampsTie_OrdersDeterministicallyAcrossSources()
    {
        await using var host = new SqliteStructuredLogTestHost();
        var timestamp = new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero);
        await host.WriteAsync(
            CreateLog("zzz", 1, "pod-b", timestamp),
            CreateLog("aaa", 2, "pod-a", timestamp));

        var result = await host.Store.QueryAsync(new() { Take = 10 });

        await Assert.That(result.Items.Select(x => x.SourceId)).IsEquivalentTo(
            ["pod-a", "pod-b"],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task ListSourcesAsync_PrefersRegistryMetadataAndHeartbeat()
    {
        await using var host = new SqliteStructuredLogTestHost(configureStructuredLogs: options =>
            options.SourceHeartbeatTimeout = TimeSpan.FromSeconds(5));
        var current = host.SourceRegistry.Current;
        await host.WriteAsync(CreateLog("local-1", 1, current.Id, DateTimeOffset.UtcNow));
        host.SourceRegistry.MarkSeen("stale-pod", DateTimeOffset.UtcNow.AddMinutes(-1));

        var sources = await host.Store.ListSourcesAsync();
        var currentSource = (await Assert.That(sources).HasSingleItem(x => x.Id == current.Id));
        var staleSource = (await Assert.That(sources).HasSingleItem(x => x.Id == "stale-pod"));

        await Assert.That(currentSource.DisplayName).IsEqualTo(current.DisplayName);
        await Assert.That(currentSource.ServiceName).IsEqualTo(current.ServiceName);
        await Assert.That(currentSource.ProcessId).IsEqualTo(current.ProcessId);
        await Assert.That(currentSource.ProcessId).IsNotEqualTo(0);
        await Assert.That(currentSource.Status).IsEqualTo(StructuredLogSourceStatus.Connected);
        await Assert.That(staleSource.Status).IsEqualTo(StructuredLogSourceStatus.Stale);
    }

    [Test]
    public async Task WriteAsync_UpdatesRegistryBeforeFlush()
    {
        await using var host = new SqliteStructuredLogTestHost();
        var receivedAt = DateTimeOffset.UtcNow;

        await host.Buffer.WriteAsync(CreateLog("buffered-1", 1, "buffered-pod", receivedAt));

        var source = (await Assert.That(await host.Store.ListSourcesAsync()).HasSingleItem(x => x.Id == "buffered-pod"));
        await Assert.That(source.LastSeen).IsEqualTo(receivedAt);
        await Assert.That(source.Status).IsEqualTo(StructuredLogSourceStatus.Connected);
        await Assert.That((await host.Store.QueryAsync(new() { Take = 10 })).Items).IsEmpty();
    }

    [Test]
    public async Task ListSourcesAsync_UsesPersistedSourcesOnlyAsFallback()
    {
        await using var firstHost = new SqliteStructuredLogTestHost();
        await firstHost.WriteAsync(CreateLog("remote-1", 1, "remote-pod", DateTimeOffset.UtcNow));
        var connectionString = firstHost.ConnectionString;

        await using var secondHost = new SqliteStructuredLogTestHost(options => options.ConnectionString = connectionString);
        var current = secondHost.SourceRegistry.Current;
        var sources = await secondHost.Store.ListSourcesAsync();
        var remote = (await Assert.That(sources).HasSingleItem(x => x.Id == "remote-pod"));
        var local = (await Assert.That(sources).HasSingleItem(x => x.Id == current.Id));

        await Assert.That(remote.DisplayName).IsEqualTo("remote-pod");
        await Assert.That(remote.ProcessId).IsEqualTo(0);
        await Assert.That(remote.Status).IsEqualTo(StructuredLogSourceStatus.Connected);
        await Assert.That(local.DisplayName).IsEqualTo(current.DisplayName);
        await Assert.That(local.ProcessId).IsEqualTo(current.ProcessId);
        await Assert.That(local.ProcessId).IsNotEqualTo(0);
    }

    [Test]
    public async Task StoreWriteManyAsync_WhenFlushIsOlder_DoesNotRegressLastSeen()
    {
        await using var host = new SqliteStructuredLogTestHost();
        var newer = DateTimeOffset.UtcNow;
        var older = newer.AddMinutes(-1);
        var store = host.Services.GetRequiredService<RelationalStructuredLogStore>();

        await host.Buffer.WriteAsync(CreateLog("newer-1", 2, "pod-b", newer));
        await store.WriteManyAsync([CreateLog("older-1", 1, "pod-b", older)]);

        var source = (await Assert.That(host.SourceRegistry.List()).HasSingleItem(x => x.Id == "pod-b"));
        await Assert.That(source.LastSeen).IsEqualTo(newer);
        await Assert.That(source.Status).IsEqualTo(StructuredLogSourceStatus.Connected);
    }

    [Test]
    public async Task ListSourcesAsync_WhenRegistryHasCaseDistinctIds_KeepsBoth()
    {
        await using var host = new SqliteStructuredLogTestHost();
        var seenAt = DateTimeOffset.UtcNow;
        host.SourceRegistry.MarkSeen("pod-a", seenAt);
        host.SourceRegistry.MarkSeen("POD-A", seenAt);

        var sources = await host.Store.ListSourcesAsync();

        await Assert.That(sources).Contains(x => x.Id == "pod-a");
        await Assert.That(sources).Contains(x => x.Id == "POD-A");
        await Assert.That(sources.Count(x => x.Id.Equals("pod-a", StringComparison.OrdinalIgnoreCase))).IsEqualTo(2);
    }

    [Test]
    public async Task StoreWriteManyAsync_MarksRegistrySeen()
    {
        await using var host = new SqliteStructuredLogTestHost();
        var store = host.Services.GetRequiredService<RelationalStructuredLogStore>();
        var receivedAt = DateTimeOffset.UtcNow;

        await store.WriteManyAsync([CreateLog("direct-1", 1, "direct-pod", receivedAt)]);

        var source = (await Assert.That(host.SourceRegistry.List()).HasSingleItem(x => x.Id == "direct-pod"));
        await Assert.That(source.LastSeen).IsEqualTo(receivedAt);
        await Assert.That(source.Status).IsEqualTo(StructuredLogSourceStatus.Connected);
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
