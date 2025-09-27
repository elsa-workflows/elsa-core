using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;
using Open.Linq.AsyncExtensions;

namespace Elsa.Persistence.EFCore.Modules.Runtime;

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

    public ValueTask<Page<StoredTrigger>> FindManyAsync(TriggerFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return FindManyAsync(filter, pageArgs, new StoredTriggerOrder<string>(x => x.Id, OrderDirection.Ascending), cancellationToken);
    }

    public async ValueTask<Page<StoredTrigger>> FindManyAsync<TProp>(TriggerFilter filter, PageArgs pageArgs, StoredTriggerOrder<TProp> order, CancellationToken cancellationToken = default)
    {
        var count = await store.QueryAsync(filter.Apply, OnLoadAsync, cancellationToken).LongCount();
        var results = await store.QueryAsync(queryable => filter.Apply(queryable).OrderBy(order).Paginate(pageArgs).OrderBy(order), OnLoadAsync, cancellationToken).ToList();
        return new(results, count);
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
        dbContext.Entry(entity).Property("SerializedPayload").CurrentValue = entity.Payload != null ? serializer.Serialize(entity.Payload) : null;
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