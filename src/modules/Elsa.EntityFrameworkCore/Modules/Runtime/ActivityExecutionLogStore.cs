using System.Text.Json;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Extensions;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Compression;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Elsa.Workflows.State;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Open.Linq.AsyncExtensions;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// An EF Core implementation of <see cref="IActivityExecutionStore"/>.
/// </summary>
[UsedImplicitly]
public class EFCoreActivityExecutionStore : IActivityExecutionStore
{
    private readonly EntityStore<RuntimeElsaDbContext, ActivityExecutionRecord> _store;
    private readonly ISafeSerializer _safeSerializer;
    private readonly IPayloadSerializer _payloadSerializer;
    private readonly ICompressionCodecResolver _compressionCodecResolver;
    private readonly IOptions<ManagementOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EFCoreActivityExecutionStore"/> class.
    /// </summary>
    public EFCoreActivityExecutionStore(
        EntityStore<RuntimeElsaDbContext, ActivityExecutionRecord> store, 
        ISafeSerializer safeSerializer, 
        IPayloadSerializer payloadSerializer,
        ICompressionCodecResolver compressionCodecResolver,
        IOptions<ManagementOptions> options)
    {
        _store = store;
        _safeSerializer = safeSerializer;
        _payloadSerializer = payloadSerializer;
        _compressionCodecResolver = compressionCodecResolver;
        _options = options;
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
        var compressionAlgorithm = _options.Value.CompressionAlgorithm ?? nameof(None);
        var serializedActivityState = entity.ActivityState != null ? await _safeSerializer.SerializeAsync(entity.ActivityState, cancellationToken) : default;
        var compressedSerializedActivityState = serializedActivityState != null ? await _compressionCodecResolver.Resolve(compressionAlgorithm).CompressAsync(serializedActivityState, cancellationToken) : default;
        
        dbContext.Entry(entity).Property("SerializedActivityState").CurrentValue = compressedSerializedActivityState;
        dbContext.Entry(entity).Property("SerializedActivityStateCompressionAlgorithm").CurrentValue = compressionAlgorithm;
        dbContext.Entry(entity).Property("SerializedOutputs").CurrentValue = entity.Outputs != null ? await _safeSerializer.SerializeAsync(entity.Outputs, cancellationToken) : default;
        dbContext.Entry(entity).Property("SerializedException").CurrentValue = entity.Exception != null ? _payloadSerializer.Serialize(entity.Exception) : default;
        dbContext.Entry(entity).Property("SerializedPayload").CurrentValue = entity.Payload != null ? _payloadSerializer.Serialize(entity.Payload) : default;
    }

    private async ValueTask OnLoadAsync(RuntimeElsaDbContext dbContext, ActivityExecutionRecord? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return;

        entity.ActivityState = await DeserializeActivityState(dbContext, entity, cancellationToken)!;
        entity.Outputs = Deserialize<IDictionary<string, object>>(dbContext, entity, "SerializedOutputs");
        entity.Exception = DeserializePayload<ExceptionState>(dbContext, entity, "SerializedException");
        entity.Payload = DeserializePayload<IDictionary<string, object>>(dbContext, entity, "SerializedPayload");
    }

    private async Task<IDictionary<string, object>>? DeserializeActivityState(RuntimeElsaDbContext dbContext, ActivityExecutionRecord entity, CancellationToken cancellationToken)
    {
        var json = dbContext.Entry(entity).Property<string>("SerializedActivityState").CurrentValue;

        if (!string.IsNullOrWhiteSpace(json))
        {
            var compressionAlgorithm = (string?)dbContext.Entry(entity).Property("SerializedActivityStateCompressionAlgorithm").CurrentValue ?? nameof(None);
            var compressionStrategy = _compressionCodecResolver.Resolve(compressionAlgorithm);
            json = await compressionStrategy.DecompressAsync(json, cancellationToken);
            var dictionary = JsonSerializer.Deserialize<IDictionary<string, object>>(json);
            return dictionary?.ToDictionary(x => x.Key, x => (object)x.Value);
        }
        
        return default;
    }
    
    private T Deserialize<T>(RuntimeElsaDbContext dbContext, ActivityExecutionRecord entity, string propertyName)
    {
        var json = dbContext.Entry(entity).Property<string>(propertyName).CurrentValue;
        var value = !string.IsNullOrEmpty(json) ? JsonSerializer.Deserialize<T>(json) : default;
        return value!;
    }

    private T DeserializePayload<T>(RuntimeElsaDbContext dbContext, ActivityExecutionRecord entity, string propertyName)
    {
        var json = dbContext.Entry(entity).Property<string>(propertyName).CurrentValue;
        var payload = !string.IsNullOrEmpty(json) ? _payloadSerializer.Deserialize<T>(json) : default;
        return payload!;
    }

    private static IQueryable<ActivityExecutionRecord> Filter(IQueryable<ActivityExecutionRecord> queryable, ActivityExecutionRecordFilter filter) => filter.Apply(queryable);
}