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
    private const string KeyPrefix = "Elsa:WorkflowDispatchOutbox:";

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken = default)
    {
        await keyValueStore.SaveAsync(new SerializedKeyValuePair
        {
            Key = GetKey(item.Id),
            SerializedValue = payloadSerializer.Serialize(item)
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
        var records = await keyValueStore.FindManyAsync(new KeyValueFilter
        {
            Key = KeyPrefix,
            StartsWith = true
        }, cancellationToken);

        var query = records
            .Select(x => payloadSerializer.Deserialize<WorkflowDispatchOutboxItem>(x.SerializedValue))
            .OfType<WorkflowDispatchOutboxItem>()
            .OrderBy(x => x.CreatedAt);

        if (maxCount > 0)
            return query.Take(maxCount).ToList();

        return query.ToList();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await keyValueStore.DeleteAsync(GetKey(id), cancellationToken);
    }

    private static string GetKey(string id) => $"{KeyPrefix}{id}";
}
