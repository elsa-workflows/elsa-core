using System.Text.Json;
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
    private readonly IActivityStateSerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EFCoreActivityExecutionStore"/> class.
    /// </summary>
    public EFCoreActivityExecutionStore(EntityStore<RuntimeElsaDbContext, ActivityExecutionRecord> store, IActivityStateSerializer serializer)
    {
        _store = store;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public async Task SaveAsync(ActivityExecutionRecord record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, OnSaveAsync, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<ActivityExecutionRecord> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, OnSaveAsync, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(order), OnLoadAsync, cancellationToken).ToList();

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(queryable => Filter(queryable, filter), OnLoadAsync, cancellationToken).ToList();

    /// <inheritdoc />
    public async Task<long> CountAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default) => await _store.CountAsync(queryable => Filter(queryable, filter), cancellationToken);

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default) => await _store.DeleteWhereAsync(queryable => Filter(queryable, filter), cancellationToken);

    private async ValueTask OnSaveAsync(RuntimeElsaDbContext dbContext, ActivityExecutionRecord entity, CancellationToken cancellationToken)
    {
        dbContext.Entry(entity).Property("ActivityData").CurrentValue = entity.ActivityState != null ? (await _serializer.SerializeAsync(entity.ActivityState, cancellationToken)).ToString() : default;
    }

    private async ValueTask OnLoadAsync(RuntimeElsaDbContext dbContext, ActivityExecutionRecord? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return;

        entity.ActivityState = await LoadActivityState(dbContext, entity);
    }

    private ValueTask<IDictionary<string, object>?> LoadActivityState(RuntimeElsaDbContext dbContext, ActivityExecutionRecord entity)
    {
        var json = dbContext.Entry(entity).Property<string>("ActivityData").CurrentValue;
        var dictionary = !string.IsNullOrEmpty(json) ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) : default;
        return ValueTask.FromResult<IDictionary<string, object>?>(dictionary?.ToDictionary(x => x.Key, x => (object)x.Value));
    }

    private IQueryable<ActivityExecutionRecord> Filter(IQueryable<ActivityExecutionRecord> queryable, ActivityExecutionRecordFilter filter) => filter.Apply(queryable);
}