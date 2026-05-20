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

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken = default)
    {
        await keyValueStore.SaveAsync(new SerializedKeyValuePair
        {
            Key = GetItemKey(item.Id),
            SerializedValue = payloadSerializer.Serialize(item)
        }, cancellationToken);

        await keyValueStore.SaveAsync(new SerializedKeyValuePair
        {
            Key = GetIndexKey(item),
            SerializedValue = item.Id
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
        var indexRecords = (await keyValueStore.FindManyAsync(new KeyValueFilter
        {
            Key = IndexKeyPrefix,
            StartsWith = true,
            OrderByKey = true,
            Take = maxCount > 0 ? maxCount : null
        }, cancellationToken)).ToList();

        var itemKeys = indexRecords.Select(x => GetItemKey(x.SerializedValue)).ToList();
        IEnumerable<SerializedKeyValuePair> itemRecords = itemKeys.Count == 0
            ? Array.Empty<SerializedKeyValuePair>()
            : await keyValueStore.FindManyAsync(new KeyValueFilter
            {
                Keys = itemKeys
            }, cancellationToken);

        var indexedItems = itemRecords
            .Select(x => payloadSerializer.Deserialize<WorkflowDispatchOutboxItem>(x.SerializedValue))
            .OfType<WorkflowDispatchOutboxItem>()
            .ToList();

        var recoverableItems = await FindRecoverableItemRecordsAsync(cancellationToken);
        var items = indexedItems
            .Concat(recoverableItems)
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

        await keyValueStore.DeleteAsync(itemKey, cancellationToken);

        if (record == null)
        {
            await keyValueStore.DeleteAsync(GetLegacyKey(id), cancellationToken);
            return;
        }

        var item = payloadSerializer.Deserialize<WorkflowDispatchOutboxItem>(record.SerializedValue);
        if (item != null)
            await keyValueStore.DeleteAsync(GetIndexKey(item), cancellationToken);
    }

    private async Task<IEnumerable<WorkflowDispatchOutboxItem>> FindRecoverableItemRecordsAsync(CancellationToken cancellationToken)
    {
        var records = await keyValueStore.FindManyAsync(new KeyValueFilter
        {
            Key = LegacyKeyPrefix,
            StartsWith = true
        }, cancellationToken);

        return records
            .Where(x => !x.Key.StartsWith(IndexKeyPrefix, StringComparison.Ordinal))
            .Select(x => payloadSerializer.Deserialize<WorkflowDispatchOutboxItem>(x.SerializedValue))
            .OfType<WorkflowDispatchOutboxItem>()
            .ToList();
    }

    private static string GetItemKey(string id) => $"{ItemKeyPrefix}{id}";

    private static string GetLegacyKey(string id) => $"{LegacyKeyPrefix}{id}";

    private static string GetIndexKey(WorkflowDispatchOutboxItem item) => $"{IndexKeyPrefix}{item.CreatedAt.UtcTicks:D20}:{item.Id}";
}
