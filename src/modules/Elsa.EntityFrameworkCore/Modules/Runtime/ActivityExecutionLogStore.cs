using System.Text.Json;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.State;
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
    private readonly ISafeSerializer _safeSerializer;
    private readonly IPayloadSerializer _payloadSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EFCoreActivityExecutionStore"/> class.
    /// </summary>
    public EFCoreActivityExecutionStore(EntityStore<RuntimeElsaDbContext, ActivityExecutionRecord> store, ISafeSerializer safeSerializer, IPayloadSerializer payloadSerializer)
    {
        _store = store;
        _safeSerializer = safeSerializer;
        _payloadSerializer = payloadSerializer;
    }

    /// <inheritdoc />
    public async Task SaveAsync(ActivityExecutionRecord record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, OnSaveAsync, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<ActivityExecutionRecord> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, OnSaveAsync, cancellationToken);

    /// <inheritdoc />
    public async Task<ActivityExecutionRecord?> FindAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.QueryAsync(queryable => Filter(queryable, filter), OnLoadAsync, cancellationToken).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(order), OnLoadAsync, cancellationToken).ToList();

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(queryable => Filter(queryable, filter), OnLoadAsync, cancellationToken).ToList();

    /// <inheritdoc />
    public async Task<long> CountAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default) => await _store.CountAsync(queryable => EFCoreActivityExecutionStore.Filter(queryable, filter), cancellationToken);

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default) => await _store.DeleteWhereAsync(queryable => EFCoreActivityExecutionStore.Filter(queryable, filter), cancellationToken);

    private async ValueTask OnSaveAsync(RuntimeElsaDbContext dbContext, ActivityExecutionRecord entity, CancellationToken cancellationToken)
    {
        dbContext.Entry(entity).Property("SerializedActivityState").CurrentValue = entity.ActivityState != null ? await _safeSerializer.SerializeAsync(entity.ActivityState, cancellationToken) : default;
        dbContext.Entry(entity).Property("SerializedOutputs").CurrentValue = entity.Outputs != null ? await _safeSerializer.SerializeAsync(entity.Outputs, cancellationToken) : default;
        dbContext.Entry(entity).Property("SerializedException").CurrentValue = entity.Exception != null ? _payloadSerializer.Serialize(entity.Exception) : default;
        dbContext.Entry(entity).Property("SerializedPayload").CurrentValue = entity.Payload != null ? _payloadSerializer.Serialize(entity.Payload) : default;
    }

    private ValueTask OnLoadAsync(RuntimeElsaDbContext dbContext, ActivityExecutionRecord? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return ValueTask.CompletedTask;

        entity.ActivityState = DeserializeActivityState(dbContext, entity);
        entity.Outputs = Deserialize<IDictionary<string, object>>(dbContext, entity, "SerializedOutputs");
        entity.Exception = DeserializePayload<ExceptionState>(dbContext, entity, "SerializedException");
        entity.Payload = DeserializePayload<IDictionary<string, object>>(dbContext, entity, "SerializedPayload");
        return ValueTask.CompletedTask;
    }

    private IDictionary<string, object>? DeserializeActivityState(RuntimeElsaDbContext dbContext, ActivityExecutionRecord entity)
    {
        var dictionary = Deserialize<Dictionary<string, JsonElement>>(dbContext, entity, "SerializedActivityState");
        return dictionary?.ToDictionary(x => x.Key, x => (object)x.Value);
    }
    
    private T? Deserialize<T>(RuntimeElsaDbContext dbContext, ActivityExecutionRecord entity, string propertyName)
    {
        var json = dbContext.Entry(entity).Property<string>(propertyName).CurrentValue;
        var value = !string.IsNullOrEmpty(json) ? JsonSerializer.Deserialize<T>(json) : default;
        return value;
    }

    private T? DeserializePayload<T>(RuntimeElsaDbContext dbContext, ActivityExecutionRecord entity, string propertyName)
    {
        var json = dbContext.Entry(entity).Property<string>(propertyName).CurrentValue;
        var payload = !string.IsNullOrEmpty(json) ? _payloadSerializer.Deserialize<T>(json) : default;
        return payload;
    }

    private static IQueryable<ActivityExecutionRecord> Filter(IQueryable<ActivityExecutionRecord> queryable, ActivityExecutionRecordFilter filter) => filter.Apply(queryable);
}