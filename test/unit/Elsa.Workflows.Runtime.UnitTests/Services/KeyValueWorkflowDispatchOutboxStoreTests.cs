using System.Text.Json;
using Elsa.Common.Services;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using Elsa.KeyValues.Stores;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Models;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class KeyValueWorkflowDispatchOutboxStoreTests
{
    private readonly IKeyValueStore _keyValueStore = Substitute.For<IKeyValueStore>();
    private readonly TestPayloadSerializer _payloadSerializer = new();
    private readonly KeyValueWorkflowDispatchOutboxStore _store;

    public KeyValueWorkflowDispatchOutboxStoreTests()
    {
        _store = new(_keyValueStore, _payloadSerializer);
        _keyValueStore.FindAsync(Arg.Any<KeyValueFilter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SerializedKeyValuePair?>(null));
        _keyValueStore.FindManyAsync(Arg.Any<KeyValueFilter>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SerializedKeyValuePair>());
    }

    [Fact]
    public async Task FindManyAsync_ReturnsOldestItemsBeforeApplyingLimit()
    {
        var newest = CreateItem("newest", new DateTimeOffset(2026, 5, 20, 12, 2, 0, TimeSpan.Zero));
        var oldest = CreateItem("oldest", new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var middle = CreateItem("middle", new DateTimeOffset(2026, 5, 20, 12, 1, 0, TimeSpan.Zero));
        _payloadSerializer.Items = new[] { newest, oldest, middle }.ToDictionary(x => x.Id);
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Index:"), Arg.Any<CancellationToken>())
            .Returns(new[] { oldest, middle }.Select(x => new SerializedKeyValuePair { Key = $"index:{x.Id}", SerializedValue = x.Id }));
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Keys != null), Arg.Any<CancellationToken>())
            .Returns(callInfo => ((KeyValueFilter)callInfo[0]).Keys!.Select(x => new SerializedKeyValuePair { Key = x, SerializedValue = x.Split(':').Last() }));

        var result = (await _store.FindManyAsync(2)).ToList();

        Assert.Equal(["oldest", "middle"], result.Select(x => x.Id));
        await _keyValueStore.Received(1).FindManyAsync(
            Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Index:" && x.OrderByKey && x.Take == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindManyAsync_MergesIndexedLegacyAndOrphanItems()
    {
        var indexed = CreateItem("indexed", new DateTimeOffset(2026, 5, 20, 12, 2, 0, TimeSpan.Zero));
        var legacy = CreateItem("legacy", new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var orphan = CreateItem("orphan", new DateTimeOffset(2026, 5, 20, 12, 1, 0, TimeSpan.Zero));
        _payloadSerializer.Items = new[] { indexed, legacy, orphan }.ToDictionary(x => x.Id);
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Index:"), Arg.Any<CancellationToken>())
            .Returns([CreateIndexRecord(indexed)]);
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Keys != null), Arg.Any<CancellationToken>())
            .Returns([CreateItemRecord(indexed)]);
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:" && x.StartsWith), Arg.Any<CancellationToken>())
            .Returns([CreateItemRecord(indexed), CreateLegacyRecord(legacy), CreateItemRecord(orphan), CreateIndexRecord(indexed)]);

        var result = (await _store.FindManyAsync()).ToList();

        Assert.Equal(["legacy", "orphan", "indexed"], result.Select(x => x.Id));
    }

    [Fact]
    public async Task FindManyAsync_AppliesLimitAfterMergingRecoverableItems()
    {
        var indexed1 = CreateItem("indexed-1", new DateTimeOffset(2026, 5, 20, 12, 1, 0, TimeSpan.Zero));
        var indexed2 = CreateItem("indexed-2", new DateTimeOffset(2026, 5, 20, 12, 2, 0, TimeSpan.Zero));
        var orphan = CreateItem("orphan", new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        _payloadSerializer.Items = new[] { indexed1, indexed2, orphan }.ToDictionary(x => x.Id);
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Index:"), Arg.Any<CancellationToken>())
            .Returns([CreateIndexRecord(indexed1), CreateIndexRecord(indexed2)]);
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Keys != null), Arg.Any<CancellationToken>())
            .Returns([CreateItemRecord(indexed1), CreateItemRecord(indexed2)]);
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:" && x.StartsWith), Arg.Any<CancellationToken>())
            .Returns([CreateItemRecord(orphan)]);

        var result = (await _store.FindManyAsync(2)).ToList();

        Assert.Equal(["orphan", "indexed-1"], result.Select(x => x.Id));
    }

    [Fact]
    public async Task FindManyAsync_MigratesOnlyCallerLimitFromLegacyScan()
    {
        var first = CreateItem("legacy-1", new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var second = CreateItem("legacy-2", new DateTimeOffset(2026, 5, 20, 12, 1, 0, TimeSpan.Zero));
        var third = CreateItem("legacy-3", new DateTimeOffset(2026, 5, 20, 12, 2, 0, TimeSpan.Zero));
        _payloadSerializer.Items = new[] { first, second, third }.ToDictionary(x => x.Id);
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:" && x.StartsWith), Arg.Any<CancellationToken>())
            .Returns([CreateLegacyRecord(first), CreateLegacyRecord(second), CreateLegacyRecord(third)]);

        var result = (await _store.FindManyAsync(2)).ToList();

        Assert.Equal(["legacy-1", "legacy-2"], result.Select(x => x.Id));
        await _keyValueStore.Received(1).SaveAsync(
            Arg.Is<SerializedKeyValuePair>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Items:legacy-1"),
            Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).SaveAsync(
            Arg.Is<SerializedKeyValuePair>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Items:legacy-2"),
            Arg.Any<CancellationToken>());
        await _keyValueStore.DidNotReceive().SaveAsync(
            Arg.Is<SerializedKeyValuePair>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Items:legacy-3"),
            Arg.Any<CancellationToken>());
        await _keyValueStore.DidNotReceive().SaveAsync(
            Arg.Is<SerializedKeyValuePair>(x => x.Key == "Elsa:WorkflowDispatchOutbox:State:LegacyScanCompleted"),
            Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).FindManyAsync(
            Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:" && x.StartsWith && x.OrderByKey && x.Take == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindManyAsync_MarksLegacyScanCompleted_WhenCallerLimitCoversRecoverableItems()
    {
        var first = CreateItem("legacy-1", new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var second = CreateItem("legacy-2", new DateTimeOffset(2026, 5, 20, 12, 1, 0, TimeSpan.Zero));
        _payloadSerializer.Items = new[] { first, second }.ToDictionary(x => x.Id);
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:" && x.StartsWith), Arg.Any<CancellationToken>())
            .Returns([CreateLegacyRecord(first), CreateLegacyRecord(second)]);

        var result = (await _store.FindManyAsync(2)).ToList();

        Assert.Equal(["legacy-1", "legacy-2"], result.Select(x => x.Id));
        await _keyValueStore.Received(1).SaveAsync(
            Arg.Is<SerializedKeyValuePair>(x => x.Key == "Elsa:WorkflowDispatchOutbox:State:LegacyScanCompleted" && x.SerializedValue == "true"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindManyAsync_UsesRecoveryMarkersWithoutBroadScan_WhenLegacyScanAlreadyCompleted()
    {
        var orphan = CreateItem("orphan", new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        _payloadSerializer.Items[orphan.Id] = orphan;
        _keyValueStore.FindAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:State:LegacyScanCompleted"), Arg.Any<CancellationToken>())
            .Returns(new SerializedKeyValuePair { Key = "Elsa:WorkflowDispatchOutbox:State:LegacyScanCompleted", SerializedValue = "true" });
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Recovery:" && x.StartsWith), Arg.Any<CancellationToken>())
            .Returns([CreateRecoveryRecord(orphan)]);
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Keys != null), Arg.Any<CancellationToken>())
            .Returns([CreateItemRecord(orphan)]);

        var result = (await _store.FindManyAsync()).ToList();

        Assert.Equal(["orphan"], result.Select(x => x.Id));
        await _keyValueStore.DidNotReceive().FindManyAsync(
            Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:" && x.StartsWith),
            Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).SaveAsync(
            Arg.Is<SerializedKeyValuePair>(x => x.Key == $"Elsa:WorkflowDispatchOutbox:Index:{orphan.CreatedAt.UtcTicks:D20}:orphan" && x.SerializedValue == "orphan"),
            Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).DeleteAsync("Elsa:WorkflowDispatchOutbox:Recovery:orphan", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindManyAsync_AppliesCallerLimitToRecoveryScan()
    {
        var first = CreateItem("orphan-1", new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var second = CreateItem("orphan-2", new DateTimeOffset(2026, 5, 20, 12, 1, 0, TimeSpan.Zero));
        _payloadSerializer.Items = new[] { first, second }.ToDictionary(x => x.Id);
        _keyValueStore.FindAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:State:LegacyScanCompleted"), Arg.Any<CancellationToken>())
            .Returns(new SerializedKeyValuePair { Key = "Elsa:WorkflowDispatchOutbox:State:LegacyScanCompleted", SerializedValue = "true" });
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Recovery:" && x.StartsWith), Arg.Any<CancellationToken>())
            .Returns([CreateRecoveryRecord(first), CreateRecoveryRecord(second)]);
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Keys != null), Arg.Any<CancellationToken>())
            .Returns([CreateItemRecord(first), CreateItemRecord(second)]);

        var result = (await _store.FindManyAsync(2)).ToList();

        Assert.Equal(["orphan-1", "orphan-2"], result.Select(x => x.Id));
        await _keyValueStore.Received(1).FindManyAsync(
            Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Recovery:" && x.StartsWith && x.OrderByKey && x.Take == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindManyAsync_CleansUpCorruptIndexedItem_AllowingLaterItemsToProgress()
    {
        var keyValueStore = new MemoryKeyValueStore(new MemoryStore<SerializedKeyValuePair>());
        var payloadSerializer = new TestPayloadSerializer();
        var store = new KeyValueWorkflowDispatchOutboxStore(keyValueStore, payloadSerializer);
        var corrupt = CreateItem("corrupt", new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var valid = CreateItem("valid", new DateTimeOffset(2026, 5, 20, 12, 1, 0, TimeSpan.Zero));
        payloadSerializer.Items[valid.Id] = valid;
        await SaveAsync(keyValueStore, CreateItemRecord(corrupt));
        await SaveAsync(keyValueStore, CreateIndexRecord(corrupt));
        await SaveAsync(keyValueStore, CreateIndexByIdRecord(corrupt));
        await SaveAsync(keyValueStore, CreateRecoveryRecord(corrupt));
        await SaveAsync(keyValueStore, CreateItemRecord(valid));
        await SaveAsync(keyValueStore, CreateIndexRecord(valid));
        await SaveAsync(keyValueStore, CreateIndexByIdRecord(valid));
        await SaveAsync(keyValueStore, CreateLegacyScanCompletedRecord());

        var firstBatch = (await store.FindManyAsync(1)).ToList();
        var secondBatch = (await store.FindManyAsync(1)).ToList();

        Assert.Empty(firstBatch);
        Assert.Equal(["valid"], secondBatch.Select(x => x.Id));
        Assert.Null(await FindAsync(keyValueStore, $"Elsa:WorkflowDispatchOutbox:Items:{corrupt.Id}"));
        Assert.Null(await FindAsync(keyValueStore, $"Elsa:WorkflowDispatchOutbox:Index:{corrupt.CreatedAt.UtcTicks:D20}:{corrupt.Id}"));
        Assert.Null(await FindAsync(keyValueStore, $"Elsa:WorkflowDispatchOutbox:IndexById:{corrupt.Id}"));
        Assert.Null(await FindAsync(keyValueStore, $"Elsa:WorkflowDispatchOutbox:Recovery:{corrupt.Id}"));
    }

    [Fact]
    public async Task FindManyAsync_CleansUpRecoveryRecord_WhenPayloadCannotBeDeserialized()
    {
        var item = CreateItem("corrupt", new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var indexKey = $"Elsa:WorkflowDispatchOutbox:Index:{item.CreatedAt.UtcTicks:D20}:{item.Id}";
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Recovery:" && x.StartsWith), Arg.Any<CancellationToken>())
            .Returns([CreateRecoveryRecord(item)]);
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Keys != null), Arg.Any<CancellationToken>())
            .Returns([CreateItemRecord(item)]);
        _keyValueStore.FindAsync(Arg.Is<KeyValueFilter>(x => x.Key == $"Elsa:WorkflowDispatchOutbox:IndexById:{item.Id}"), Arg.Any<CancellationToken>())
            .Returns(new SerializedKeyValuePair { Key = $"Elsa:WorkflowDispatchOutbox:IndexById:{item.Id}", SerializedValue = indexKey });

        var result = (await _store.FindManyAsync()).ToList();

        Assert.Empty(result);
        await _keyValueStore.Received(1).DeleteAsync(indexKey, Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).DeleteAsync($"Elsa:WorkflowDispatchOutbox:IndexById:{item.Id}", Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).DeleteAsync($"Elsa:WorkflowDispatchOutbox:Recovery:{item.Id}", Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).DeleteAsync($"Elsa:WorkflowDispatchOutbox:Items:{item.Id}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_WritesItemAndSortableIndex()
    {
        var item = CreateItem("outbox-1", new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        item.TenantId = "tenant-1";

        await _store.SaveAsync(item);

        await _keyValueStore.Received(1).SaveAsync(
            Arg.Is<SerializedKeyValuePair>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Recovery:outbox-1" && x.SerializedValue == "outbox-1" && x.TenantId == "tenant-1"),
            Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).SaveAsync(
            Arg.Is<SerializedKeyValuePair>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Items:outbox-1" && x.SerializedValue == "outbox-1" && x.TenantId == "tenant-1"),
            Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).SaveAsync(
            Arg.Is<SerializedKeyValuePair>(x => x.Key == $"Elsa:WorkflowDispatchOutbox:Index:{item.CreatedAt.UtcTicks:D20}:outbox-1" && x.SerializedValue == "outbox-1" && x.TenantId == "tenant-1"),
            Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).SaveAsync(
            Arg.Is<SerializedKeyValuePair>(x => x.Key == "Elsa:WorkflowDispatchOutbox:IndexById:outbox-1" && x.SerializedValue == $"Elsa:WorkflowDispatchOutbox:Index:{item.CreatedAt.UtcTicks:D20}:outbox-1" && x.TenantId == "tenant-1"),
            Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).DeleteAsync("Elsa:WorkflowDispatchOutbox:Recovery:outbox-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_RemovesIndexBeforeItem()
    {
        var item = CreateItem("outbox-1", new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        _payloadSerializer.Items[item.Id] = item;
        _keyValueStore.FindAsync(Arg.Any<KeyValueFilter>(), Arg.Any<CancellationToken>())
            .Returns(new SerializedKeyValuePair { Key = "Elsa:WorkflowDispatchOutbox:Items:outbox-1", SerializedValue = item.Id });

        await _store.DeleteAsync(item.Id);

        Received.InOrder(() =>
        {
            _keyValueStore.DeleteAsync($"Elsa:WorkflowDispatchOutbox:Index:{item.CreatedAt.UtcTicks:D20}:outbox-1", Arg.Any<CancellationToken>());
            _keyValueStore.DeleteAsync("Elsa:WorkflowDispatchOutbox:Items:outbox-1", Arg.Any<CancellationToken>());
        });
        await _keyValueStore.Received(1).DeleteAsync("Elsa:WorkflowDispatchOutbox:Items:outbox-1", Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).DeleteAsync($"Elsa:WorkflowDispatchOutbox:Index:{item.CreatedAt.UtcTicks:D20}:outbox-1", Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).DeleteAsync("Elsa:WorkflowDispatchOutbox:IndexById:outbox-1", Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).DeleteAsync("Elsa:WorkflowDispatchOutbox:Recovery:outbox-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_CleansUpIndexRecords_WhenItemRecordIsMissing()
    {
        var item = CreateItem("outbox-1", new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        _keyValueStore.FindAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Items:outbox-1"), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SerializedKeyValuePair?>(null));
        _keyValueStore.FindAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:IndexById:outbox-1"), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SerializedKeyValuePair?>(null));
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Index:" && x.StartsWith), Arg.Any<CancellationToken>())
            .Returns([CreateIndexRecord(item), CreateIndexRecord(CreateItem("outbox-2", item.CreatedAt))]);

        await _store.DeleteAsync(item.Id);

        await _keyValueStore.Received(1).DeleteAsync($"Elsa:WorkflowDispatchOutbox:Index:{item.CreatedAt.UtcTicks:D20}:outbox-1", Arg.Any<CancellationToken>());
        await _keyValueStore.DidNotReceive().DeleteAsync($"Elsa:WorkflowDispatchOutbox:Index:{item.CreatedAt.UtcTicks:D20}:outbox-2", Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).DeleteAsync("Elsa:WorkflowDispatchOutbox:Items:outbox-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_CleansUpIndexRecords_WhenItemPayloadCannotBeDeserialized()
    {
        var item = CreateItem("outbox-1", new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var indexKey = $"Elsa:WorkflowDispatchOutbox:Index:{item.CreatedAt.UtcTicks:D20}:outbox-1";
        _keyValueStore.FindAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Items:outbox-1"), Arg.Any<CancellationToken>())
            .Returns(new SerializedKeyValuePair { Key = "Elsa:WorkflowDispatchOutbox:Items:outbox-1", SerializedValue = "invalid" });
        _keyValueStore.FindAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:IndexById:outbox-1"), Arg.Any<CancellationToken>())
            .Returns(new SerializedKeyValuePair { Key = "Elsa:WorkflowDispatchOutbox:IndexById:outbox-1", SerializedValue = indexKey });

        await _store.DeleteAsync(item.Id);

        await _keyValueStore.Received(1).DeleteAsync(indexKey, Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).DeleteAsync("Elsa:WorkflowDispatchOutbox:IndexById:outbox-1", Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).DeleteAsync("Elsa:WorkflowDispatchOutbox:Items:outbox-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_UsesIndexByIdBeforeScanningAllIndexes()
    {
        var item = CreateItem("outbox-1", new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var indexKey = $"Elsa:WorkflowDispatchOutbox:Index:{item.CreatedAt.UtcTicks:D20}:outbox-1";
        _keyValueStore.FindAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Items:outbox-1"), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SerializedKeyValuePair?>(null));
        _keyValueStore.FindAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:IndexById:outbox-1"), Arg.Any<CancellationToken>())
            .Returns(new SerializedKeyValuePair { Key = "Elsa:WorkflowDispatchOutbox:IndexById:outbox-1", SerializedValue = indexKey });

        await _store.DeleteAsync(item.Id);

        await _keyValueStore.Received(1).DeleteAsync(indexKey, Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).DeleteAsync("Elsa:WorkflowDispatchOutbox:IndexById:outbox-1", Arg.Any<CancellationToken>());
        await _keyValueStore.DidNotReceive().FindManyAsync(
            Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Index:" && x.StartsWith),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_UsesScan_WhenIndexByIdValueDoesNotMatchExpectedId()
    {
        var item = CreateItem("outbox-1", new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var mismatchedIndexKey = $"Elsa:WorkflowDispatchOutbox:Index:{item.CreatedAt.UtcTicks:D20}:outbox-2";
        _keyValueStore.FindAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Items:outbox-1"), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SerializedKeyValuePair?>(null));
        _keyValueStore.FindAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:IndexById:outbox-1"), Arg.Any<CancellationToken>())
            .Returns(new SerializedKeyValuePair { Key = "Elsa:WorkflowDispatchOutbox:IndexById:outbox-1", SerializedValue = mismatchedIndexKey });
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Index:" && x.StartsWith), Arg.Any<CancellationToken>())
            .Returns([CreateIndexRecord(item)]);

        await _store.DeleteAsync(item.Id);

        await _keyValueStore.DidNotReceive().DeleteAsync(mismatchedIndexKey, Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).DeleteAsync($"Elsa:WorkflowDispatchOutbox:Index:{item.CreatedAt.UtcTicks:D20}:outbox-1", Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).DeleteAsync("Elsa:WorkflowDispatchOutbox:IndexById:outbox-1", Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).FindManyAsync(
            Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Index:" && x.StartsWith),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_UsesScan_WhenIndexByIdValueIsNotWellFormed()
    {
        var item = CreateItem("outbox-1", new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        _keyValueStore.FindAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Items:outbox-1"), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SerializedKeyValuePair?>(null));
        _keyValueStore.FindAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:IndexById:outbox-1"), Arg.Any<CancellationToken>())
            .Returns(new SerializedKeyValuePair { Key = "Elsa:WorkflowDispatchOutbox:IndexById:outbox-1", SerializedValue = "arbitrary-key" });
        _keyValueStore.FindManyAsync(Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Index:" && x.StartsWith), Arg.Any<CancellationToken>())
            .Returns([CreateIndexRecord(item)]);

        await _store.DeleteAsync(item.Id);

        await _keyValueStore.DidNotReceive().DeleteAsync("arbitrary-key", Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).DeleteAsync($"Elsa:WorkflowDispatchOutbox:Index:{item.CreatedAt.UtcTicks:D20}:outbox-1", Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).DeleteAsync("Elsa:WorkflowDispatchOutbox:IndexById:outbox-1", Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).FindManyAsync(
            Arg.Is<KeyValueFilter>(x => x.Key == "Elsa:WorkflowDispatchOutbox:Index:" && x.StartsWith),
            Arg.Any<CancellationToken>());
    }

    private static WorkflowDispatchOutboxItem CreateItem(string id, DateTimeOffset createdAt)
    {
        return new()
        {
            Id = id,
            OwnerWorkflowInstanceId = "parent-1",
            Kind = WorkflowDispatchOutboxItemKind.WorkflowDefinition,
            WorkflowDefinitionCommand = new DispatchWorkflowDefinitionCommand("definition-version-1"),
            CreatedAt = createdAt
        };
    }

    private static SerializedKeyValuePair CreateItemRecord(WorkflowDispatchOutboxItem item) => new()
    {
        Key = $"Elsa:WorkflowDispatchOutbox:Items:{item.Id}",
        SerializedValue = item.Id
    };

    private static SerializedKeyValuePair CreateLegacyRecord(WorkflowDispatchOutboxItem item) => new()
    {
        Key = $"Elsa:WorkflowDispatchOutbox:{item.Id}",
        SerializedValue = item.Id
    };

    private static SerializedKeyValuePair CreateIndexRecord(WorkflowDispatchOutboxItem item) => new()
    {
        Key = $"Elsa:WorkflowDispatchOutbox:Index:{item.CreatedAt.UtcTicks:D20}:{item.Id}",
        SerializedValue = item.Id
    };

    private static SerializedKeyValuePair CreateIndexByIdRecord(WorkflowDispatchOutboxItem item) => new()
    {
        Key = $"Elsa:WorkflowDispatchOutbox:IndexById:{item.Id}",
        SerializedValue = $"Elsa:WorkflowDispatchOutbox:Index:{item.CreatedAt.UtcTicks:D20}:{item.Id}"
    };

    private static SerializedKeyValuePair CreateRecoveryRecord(WorkflowDispatchOutboxItem item) => new()
    {
        Key = $"Elsa:WorkflowDispatchOutbox:Recovery:{item.Id}",
        SerializedValue = item.Id
    };

    private static SerializedKeyValuePair CreateLegacyScanCompletedRecord() => new()
    {
        Key = "Elsa:WorkflowDispatchOutbox:State:LegacyScanCompleted",
        SerializedValue = "true"
    };

    private static async Task SaveAsync(IKeyValueStore keyValueStore, SerializedKeyValuePair record)
    {
        await keyValueStore.SaveAsync(record, CancellationToken.None);
    }

    private static async Task<SerializedKeyValuePair?> FindAsync(IKeyValueStore keyValueStore, string key)
    {
        return await keyValueStore.FindAsync(new KeyValueFilter { Key = key }, CancellationToken.None);
    }

    private class TestPayloadSerializer : IPayloadSerializer
    {
        public IDictionary<string, WorkflowDispatchOutboxItem> Items { get; set; } = new Dictionary<string, WorkflowDispatchOutboxItem>();

        public string Serialize(object payload) => ((WorkflowDispatchOutboxItem)payload).Id;

        public JsonElement SerializeToElement(object payload) => throw new NotSupportedException();

        public object Deserialize(string serializedData) => Deserialize<WorkflowDispatchOutboxItem>(serializedData);

        public object Deserialize(string serializedData, Type type) => Deserialize(serializedData);

        public object Deserialize(JsonElement serializedData) => throw new NotSupportedException();

        public T Deserialize<T>(string serializedData) => Items.TryGetValue(serializedData, out var item) ? (T)(object)item : default!;

        public T Deserialize<T>(JsonElement serializedData) => throw new NotSupportedException();

        public JsonSerializerOptions GetOptions() => new();
    }
}
