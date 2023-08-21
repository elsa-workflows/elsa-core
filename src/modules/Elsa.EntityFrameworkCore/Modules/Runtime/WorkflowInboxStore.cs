using Elsa.Common.Contracts;
using Elsa.Common.Models;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// An EF Core implementation of <see cref="IBookmarkStore"/>.
/// </summary>
public class EFCoreWorkflowInboxMessageStore : IWorkflowInboxMessageStore
{
    private readonly EntityStore<RuntimeElsaDbContext, WorkflowInboxMessage> _store;
    private readonly IPayloadSerializer _payloadSerializer;
    private readonly ISystemClock _systemClock;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreWorkflowInboxMessageStore(EntityStore<RuntimeElsaDbContext, WorkflowInboxMessage> store, IPayloadSerializer payloadSerializer, ISystemClock systemClock)
    {
        _store = store;
        _payloadSerializer = payloadSerializer;
        _systemClock = systemClock;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(WorkflowInboxMessage record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, OnSaveAsync, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(WorkflowInboxMessageFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.QueryAsync(q => filter.Apply(q, _systemClock.UtcNow), OnLoadAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(IEnumerable<WorkflowInboxMessageFilter> filters, CancellationToken cancellationToken = default)
    {
        return await _store.QueryAsync(query =>
        {
            foreach (var filter in filters) filter.Apply(query, _systemClock.UtcNow);
            return query;
        }, OnLoadAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteManyAsync(WorkflowInboxMessageFilter filter, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        return await _store.DeleteWhereAsync(q => Paginate(filter.Apply(q, _systemClock.UtcNow), pageArgs), cancellationToken);
    }

    private ValueTask OnSaveAsync(RuntimeElsaDbContext dbContext, WorkflowInboxMessage entity, CancellationToken cancellationToken)
    {
        dbContext.Entry(entity).Property("SerializedBookmarkPayload").CurrentValue = _payloadSerializer.Serialize(entity.BookmarkPayload);
        dbContext.Entry(entity).Property("SerializedInput").CurrentValue = entity.Input != null ? _payloadSerializer.Serialize(entity.Input) : default;
        return default;
    }

    private ValueTask OnLoadAsync(RuntimeElsaDbContext dbContext, WorkflowInboxMessage? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return ValueTask.CompletedTask;

        var bookmarkPayloadJson = dbContext.Entry(entity).Property<string>("SerializedBookmarkPayload").CurrentValue;
        var inputJson = dbContext.Entry(entity).Property<string>("SerializedInput").CurrentValue;

        entity.BookmarkPayload = _payloadSerializer.Deserialize(bookmarkPayloadJson);
        entity.Input = !string.IsNullOrEmpty(inputJson) ? _payloadSerializer.Deserialize<Dictionary<string, object>>(inputJson) : null;

        return ValueTask.CompletedTask;
    }

    private static IQueryable<WorkflowInboxMessage> Paginate(IQueryable<WorkflowInboxMessage> queryable, PageArgs? pageArgs)
    {
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);
        return queryable;
    }
}