using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Stores workflow dispatch outbox items in the existing key-value store.
/// </summary>
public class KeyValueWorkflowDispatchOutboxStore(IKeyValueStore keyValueStore, IPayloadSerializer payloadSerializer) : IWorkflowDispatchOutboxStore
{
    private const string LegacyKeyPrefix = "Elsa:WorkflowDispatchOutbox:";
    private const string ItemKeyPrefix = "Elsa:WorkflowDispatchOutbox:Items:";
    private const string IndexKeyPrefix = "Elsa:WorkflowDispatchOutbox:Index:";
    private const string IndexByIdKeyPrefix = "Elsa:WorkflowDispatchOutbox:IndexById:";
    private const string RecoveryKeyPrefix = "Elsa:WorkflowDispatchOutbox:Recovery:";
    private const string StateKeyPrefix = "Elsa:WorkflowDispatchOutbox:State:";
    private const string LegacyScanCompletedKey = $"{StateKeyPrefix}LegacyScanCompleted";

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken = default)
    {
        await keyValueStore.SaveAsync(new SerializedKeyValuePair
        {
            Key = GetRecoveryKey(item.Id),
            SerializedValue = item.Id
        }, cancellationToken);

        await keyValueStore.SaveAsync(new SerializedKeyValuePair
        {
            Key = GetItemKey(item.Id),
            SerializedValue = payloadSerializer.Serialize(item)
        }, cancellationToken);

        await SaveIndexAsync(item, cancellationToken);
        await keyValueStore.DeleteAsync(GetRecoveryKey(item.Id), cancellationToken);
    }

    private async Task SaveIndexAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken)
    {
        var indexKey = GetIndexKey(item);

        await keyValueStore.SaveAsync(new SerializedKeyValuePair
        {
            Key = indexKey,
            SerializedValue = item.Id
        }, cancellationToken);

        await keyValueStore.SaveAsync(new SerializedKeyValuePair
        {
            Key = GetIndexByIdKey(item.Id),
            SerializedValue = indexKey
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDispatchOutboxItem>> FindManyAsync(CancellationToken cancellationToken = default)
    {
        return await FindManyAsync(maxCount: 0, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDispatchOutboxItem>> FindManyAsync(int maxCount, CancellationToken cancellationToken = default)
    {
        var indexedItems = await FindIndexedItemsAsync(maxCount, cancellationToken);
        var recoverableItems = await FindRecoveryItemsAsync(cancellationToken);
        var legacyItems = await FindLegacyItemsAsync(maxCount, cancellationToken);
        var items = indexedItems
            .Concat(recoverableItems)
            .Concat(legacyItems)
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .OrderBy(x => x.CreatedAt);

        if (maxCount > 0)
            return items.Take(maxCount).ToList();

        return items.ToList();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var itemKey = GetItemKey(id);
        var record = await keyValueStore.FindAsync(new KeyValueFilter { Key = itemKey }, cancellationToken);

        if (record == null)
        {
            await DeleteIndexesForMissingItemAsync(id, cancellationToken);
            await keyValueStore.DeleteAsync(GetRecoveryKey(id), cancellationToken);
            await keyValueStore.DeleteAsync(GetLegacyKey(id), cancellationToken);
            await keyValueStore.DeleteAsync(itemKey, cancellationToken);
            return;
        }

        var item = payloadSerializer.Deserialize<WorkflowDispatchOutboxItem>(record.SerializedValue);

        if (item != null)
        {
            await keyValueStore.DeleteAsync(GetIndexKey(item), cancellationToken);
            await keyValueStore.DeleteAsync(GetIndexByIdKey(id), cancellationToken);
        }
        else
        {
            await DeleteIndexesForMissingItemAsync(id, cancellationToken);
        }

        await keyValueStore.DeleteAsync(GetRecoveryKey(id), cancellationToken);
        await keyValueStore.DeleteAsync(GetLegacyKey(id), cancellationToken);
        await keyValueStore.DeleteAsync(itemKey, cancellationToken);
    }

    private async Task<IEnumerable<WorkflowDispatchOutboxItem>> FindIndexedItemsAsync(int maxCount, CancellationToken cancellationToken)
    {
        var indexRecords = (await keyValueStore.FindManyAsync(new KeyValueFilter
        {
            Key = IndexKeyPrefix,
            StartsWith = true,
            OrderByKey = true,
            Take = maxCount > 0 ? maxCount : null
        }, cancellationToken)).ToList();
        var itemKeys = indexRecords.Select(x => GetItemKey(x.SerializedValue)).ToList();

        var itemRecords = itemKeys.Count == 0
            ? Array.Empty<SerializedKeyValuePair>()
            : await keyValueStore.FindManyAsync(new KeyValueFilter
            {
                Keys = itemKeys
            }, cancellationToken);
        var itemRecordLookup = itemRecords.ToDictionary(x => x.Key);
        var items = new List<WorkflowDispatchOutboxItem>();

        foreach (var indexRecord in indexRecords)
        {
            var id = indexRecord.SerializedValue;
            var itemKey = GetItemKey(id);

            if (!itemRecordLookup.TryGetValue(itemKey, out var itemRecord))
            {
                await keyValueStore.DeleteAsync(indexRecord.Key, cancellationToken);
                await keyValueStore.DeleteAsync(GetIndexByIdKey(id), cancellationToken);
                continue;
            }

            var item = payloadSerializer.Deserialize<WorkflowDispatchOutboxItem>(itemRecord.SerializedValue);

            if (item == null)
            {
                await DeleteUnrecoverableItemAsync(id, itemKey, indexRecord.Key, recoveryKey: null, cancellationToken: cancellationToken);
                continue;
            }

            items.Add(item);
        }

        return items;
    }

    private async Task<IEnumerable<WorkflowDispatchOutboxItem>> FindRecoveryItemsAsync(CancellationToken cancellationToken)
    {
        var recoveryRecords = (await keyValueStore.FindManyAsync(new KeyValueFilter
        {
            Key = RecoveryKeyPrefix,
            StartsWith = true
        }, cancellationToken)).ToList();

        if (recoveryRecords.Count == 0)
            return [];

        var itemKeys = recoveryRecords.Select(x => GetItemKey(x.SerializedValue)).ToList();
        var itemRecords = (await keyValueStore.FindManyAsync(new KeyValueFilter { Keys = itemKeys }, cancellationToken)).ToDictionary(x => x.Key);
        var items = new List<WorkflowDispatchOutboxItem>();

        foreach (var recoveryRecord in recoveryRecords)
        {
            var id = recoveryRecord.SerializedValue;

            if (!itemRecords.TryGetValue(GetItemKey(id), out var itemRecord))
            {
                await DeleteIndexesForMissingItemAsync(id, cancellationToken);
                await keyValueStore.DeleteAsync(recoveryRecord.Key, cancellationToken);
                continue;
            }

            var item = payloadSerializer.Deserialize<WorkflowDispatchOutboxItem>(itemRecord.SerializedValue);

            if (item == null)
            {
                await DeleteUnrecoverableItemAsync(id, itemRecord.Key, indexKey: null, recoveryKey: recoveryRecord.Key, cancellationToken: cancellationToken);
                continue;
            }

            await SaveIndexAsync(item, cancellationToken);
            await keyValueStore.DeleteAsync(recoveryRecord.Key, cancellationToken);
            items.Add(item);
        }

        return items;
    }

    private async Task<IEnumerable<WorkflowDispatchOutboxItem>> FindLegacyItemsAsync(int maxCount, CancellationToken cancellationToken)
    {
        var legacyScanCompleted = await keyValueStore.FindAsync(new KeyValueFilter { Key = LegacyScanCompletedKey }, cancellationToken);

        if (legacyScanCompleted != null)
            return [];

        var records = await keyValueStore.FindManyAsync(new KeyValueFilter
        {
            Key = LegacyKeyPrefix,
            StartsWith = true
        }, cancellationToken);

        var recoverableItems = records
            .Where(IsRecoverableItemRecord)
            .Select(x => new
            {
                Record = x,
                Item = payloadSerializer.Deserialize<WorkflowDispatchOutboxItem>(x.SerializedValue)
            })
            .Where(x => x.Item != null)
            .ToList();
        var recoverableItemsToMigrate = maxCount > 0 ? recoverableItems.Take(maxCount).ToList() : recoverableItems;
        var items = new List<WorkflowDispatchOutboxItem>();

        foreach (var recoverableItem in recoverableItemsToMigrate)
        {
            var record = recoverableItem.Record;
            var item = recoverableItem.Item!;
            await SaveAsync(item, cancellationToken);

            if (!record.Key.StartsWith(ItemKeyPrefix, StringComparison.Ordinal))
                await keyValueStore.DeleteAsync(record.Key, cancellationToken);

            items.Add(item);
        }

        if (recoverableItemsToMigrate.Count == recoverableItems.Count)
        {
            await keyValueStore.SaveAsync(new SerializedKeyValuePair
            {
                Key = LegacyScanCompletedKey,
                SerializedValue = "true"
            }, cancellationToken);
        }

        return items;
    }

    private async Task DeleteIndexesForMissingItemAsync(string id, CancellationToken cancellationToken)
    {
        var lookupRecord = await keyValueStore.FindAsync(new KeyValueFilter { Key = GetIndexByIdKey(id) }, cancellationToken);

        if (lookupRecord != null)
        {
            if (IsIndexKeyForId(lookupRecord.SerializedValue, id))
            {
                await keyValueStore.DeleteAsync(lookupRecord.SerializedValue, cancellationToken);
                await keyValueStore.DeleteAsync(lookupRecord.Key, cancellationToken);
                return;
            }

            await keyValueStore.DeleteAsync(lookupRecord.Key, cancellationToken);
        }

        var matchingIndexRecords = await keyValueStore.FindManyAsync(new KeyValueFilter
        {
            Key = IndexKeyPrefix,
            StartsWith = true
        }, cancellationToken);

        foreach (var indexRecord in matchingIndexRecords.Where(x => x.SerializedValue == id || x.Key.EndsWith($":{id}", StringComparison.Ordinal)))
        {
            await keyValueStore.DeleteAsync(indexRecord.Key, cancellationToken);
        }
    }

    private async Task DeleteUnrecoverableItemAsync(string id, string itemKey, string? indexKey, string? recoveryKey, CancellationToken cancellationToken)
    {
        if (indexKey != null)
        {
            await keyValueStore.DeleteAsync(indexKey, cancellationToken);
            await keyValueStore.DeleteAsync(GetIndexByIdKey(id), cancellationToken);
        }
        else
        {
            await DeleteIndexesForMissingItemAsync(id, cancellationToken);
        }

        await keyValueStore.DeleteAsync(recoveryKey ?? GetRecoveryKey(id), cancellationToken);
        await keyValueStore.DeleteAsync(GetLegacyKey(id), cancellationToken);
        await keyValueStore.DeleteAsync(itemKey, cancellationToken);
    }

    private static string GetItemKey(string id) => $"{ItemKeyPrefix}{id}";

    private static string GetLegacyKey(string id) => $"{LegacyKeyPrefix}{id}";

    private static string GetIndexKey(WorkflowDispatchOutboxItem item) => $"{IndexKeyPrefix}{item.CreatedAt.UtcTicks:D20}:{item.Id}";

    private static string GetIndexByIdKey(string id) => $"{IndexByIdKeyPrefix}{id}";

    private static string GetRecoveryKey(string id) => $"{RecoveryKeyPrefix}{id}";

    private static bool IsIndexKeyForId(string indexKey, string id)
    {
        var expectedSuffix = $":{id}";

        if (!indexKey.StartsWith(IndexKeyPrefix, StringComparison.Ordinal) || !indexKey.EndsWith(expectedSuffix, StringComparison.Ordinal))
            return false;

        var ticksStart = IndexKeyPrefix.Length;
        var ticksLength = indexKey.Length - ticksStart - expectedSuffix.Length;

        if (ticksLength != 20)
            return false;

        foreach (var character in indexKey.AsSpan(ticksStart, ticksLength))
        {
            if (character is < '0' or > '9')
                return false;
        }

        return true;
    }

    private static bool IsRecoverableItemRecord(SerializedKeyValuePair record)
    {
        return !record.Key.StartsWith(IndexKeyPrefix, StringComparison.Ordinal)
               && !record.Key.StartsWith(IndexByIdKeyPrefix, StringComparison.Ordinal)
               && !record.Key.StartsWith(RecoveryKeyPrefix, StringComparison.Ordinal)
               && !record.Key.StartsWith(StateKeyPrefix, StringComparison.Ordinal);
    }
}
