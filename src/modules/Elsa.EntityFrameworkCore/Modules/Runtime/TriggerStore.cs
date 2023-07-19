using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <inheritdoc />
public class EFCoreTriggerStore : ITriggerStore
{
    private readonly EntityStore<RuntimeElsaDbContext, StoredTrigger> _store;
    private readonly IPayloadSerializer _serializer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreTriggerStore(EntityStore<RuntimeElsaDbContext, StoredTrigger> store, IPayloadSerializer serializer)
    {
        _store = store;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, SaveAsync, cancellationToken);

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, SaveAsync, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default) => await _store.QueryAsync(filter.Apply, LoadAsync, cancellationToken);

    /// <inheritdoc />
    public async ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default)
    {
        var filter = new TriggerFilter { Ids = removed.Select(r => r.Id).ToList() };
        await DeleteManyAsync(filter, cancellationToken);
        await _store.SaveManyAsync(added, SaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default) =>
        await _store.DeleteWhereAsync(filter.Apply, cancellationToken);
    
    private ValueTask<StoredTrigger> SaveAsync(RuntimeElsaDbContext dbContext, StoredTrigger entity, CancellationToken cancellationToken)
    {
        dbContext.Entry(entity).Property("Data").CurrentValue = entity.Payload != null ? _serializer.Serialize(entity.Payload) : default;
        return ValueTask.FromResult(entity);
    }

    private ValueTask<StoredTrigger?> LoadAsync(RuntimeElsaDbContext dbContext, StoredTrigger? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return ValueTask.FromResult(entity);

        var json = dbContext.Entry(entity).Property<string>("Data").CurrentValue;
        entity.Payload = !string.IsNullOrEmpty(json) ? _serializer.Deserialize(json) : null;

        return new(entity);
    }
}