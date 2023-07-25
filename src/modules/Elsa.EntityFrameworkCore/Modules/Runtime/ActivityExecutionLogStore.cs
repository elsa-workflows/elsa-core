using Elsa.EntityFrameworkCore.Common;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Open.Linq.AsyncExtensions;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// An EF Core implementation of <see cref="IActivityExecutionStore"/>.
/// </summary>
public class EFCoreActivityExecutionStore : IActivityExecutionStore
{
    private readonly EntityStore<RuntimeElsaDbContext, ActivityExecutionRecord> _store;
    private readonly IPayloadSerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EFCoreActivityExecutionStore"/> class.
    /// </summary>
    public EFCoreActivityExecutionStore(EntityStore<RuntimeElsaDbContext, ActivityExecutionRecord> store, IPayloadSerializer serializer)
    {
        _store = store;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public async Task SaveAsync(ActivityExecutionRecord record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, SaveAsync, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<ActivityExecutionRecord> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, SaveAsync, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(order), LoadAsync, cancellationToken).ToList();

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(queryable => Filter(queryable, filter), LoadAsync, cancellationToken).ToList();

    /// <inheritdoc />
    public async Task<long> CountAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default) => await _store.CountAsync(queryable => Filter(queryable, filter), cancellationToken);

    private ValueTask<ActivityExecutionRecord> SaveAsync(RuntimeElsaDbContext dbContext, ActivityExecutionRecord entity, CancellationToken cancellationToken)
    {
        dbContext.Entry(entity).Property("ActivityData").CurrentValue = entity.ActivityState != null ? _serializer.Serialize(entity.ActivityState) : default;
        return new(entity);
    }

    private async ValueTask<ActivityExecutionRecord?> LoadAsync(RuntimeElsaDbContext dbContext, ActivityExecutionRecord? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return entity;

        entity.ActivityState = await LoadActivityState(dbContext, entity);

        return entity;
    }

    private ValueTask<IDictionary<string, object>?> LoadActivityState(RuntimeElsaDbContext dbContext, ActivityExecutionRecord entity)
    {
        var json = dbContext.Entry(entity).Property<string>("ActivityData").CurrentValue;
        return new(!string.IsNullOrEmpty(json) ? _serializer.Deserialize<IDictionary<string, object>>(json) : null);
    }

    private IQueryable<ActivityExecutionRecord> Filter(IQueryable<ActivityExecutionRecord> queryable, ActivityExecutionRecordFilter filter) => filter.Apply(queryable);
}