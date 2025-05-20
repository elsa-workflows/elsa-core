using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <inheritdoc />
[UsedImplicitly]
public class EFCoreTriggerStore(EntityStore<RuntimeElsaDbContext, StoredTrigger> store, IPayloadSerializer serializer) : ITriggerStore
{
    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default)
    {
        await store.SaveAsync(record, OnSaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default)
    {
        await store.SaveManyAsync(records, OnSaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<StoredTrigger?> FindAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.FindAsync(filter.Apply, OnLoadAsync, filter.TenantAgnostic, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(filter.Apply, OnLoadAsync, filter.TenantAgnostic, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default)
    {
        var removedList = removed.ToList();
        
        if(removedList.Count > 0)
        {
            var filter = new TriggerFilter { Ids = removedList.Select(r => r.Id).ToList() };
            await DeleteManyAsync(filter, cancellationToken);
        }
        
        await store.SaveManyAsync(added, OnSaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteWhereAsync(filter.Apply, cancellationToken);
    }

    private ValueTask OnSaveAsync(RuntimeElsaDbContext dbContext, StoredTrigger entity, CancellationToken cancellationToken)
    {
        dbContext.Entry(entity).Property("SerializedPayload").CurrentValue = entity.Payload != null ? serializer.Serialize(entity.Payload) : default;
        return default;
    }

    private ValueTask OnLoadAsync(RuntimeElsaDbContext dbContext, StoredTrigger? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return ValueTask.CompletedTask;

        var json = dbContext.Entry(entity).Property<string>("SerializedPayload").CurrentValue;
        entity.Payload = !string.IsNullOrEmpty(json) ? serializer.Deserialize(json) : null;

        return ValueTask.CompletedTask;
    }
}