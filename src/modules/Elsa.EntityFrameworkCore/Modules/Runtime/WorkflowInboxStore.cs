using System.Text.Json;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// An EF Core implementation of <see cref="IBookmarkStore"/>.
/// </summary>
public class EFCoreWorkflowInboxStore : IWorkflowInboxStore
{
    private readonly EntityStore<RuntimeElsaDbContext, WorkflowInboxMessage> _store;
    private readonly IPayloadSerializer _payloadSerializer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreWorkflowInboxStore(EntityStore<RuntimeElsaDbContext, WorkflowInboxMessage> store, IPayloadSerializer payloadSerializer)
    {
        _store = store;
        _payloadSerializer = payloadSerializer;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(WorkflowInboxMessage record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, SaveAsync, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(WorkflowInboxMessageFilter filter, CancellationToken cancellationToken = default) => await _store.QueryAsync(filter.Apply, LoadAsync, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(WorkflowInboxMessageFilter filter, CancellationToken cancellationToken = default) => await _store.DeleteWhereAsync(filter.Apply, cancellationToken);
    
    private ValueTask<WorkflowInboxMessage> SaveAsync(RuntimeElsaDbContext dbContext, WorkflowInboxMessage entity, CancellationToken cancellationToken)
    {
        dbContext.Entry(entity).Property("SerializedBookmarkPayload").CurrentValue = _payloadSerializer.Serialize(entity.BookmarkPayload);
        dbContext.Entry(entity).Property("SerializedInput").CurrentValue = entity.Input != null ? _payloadSerializer.Serialize(entity.Input) : default;
        dbContext.Entry(entity).Property("SerializedAffectedWorkflowInstancesIds").CurrentValue = entity.AffectedWorkflowInstancesIds != null ? JsonSerializer.Serialize(entity.AffectedWorkflowInstancesIds) : default;
        return new(entity);
    }

    private ValueTask<WorkflowInboxMessage?> LoadAsync(RuntimeElsaDbContext dbContext, WorkflowInboxMessage? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return ValueTask.FromResult(entity);

        var bookmarkPayloadJson = dbContext.Entry(entity).Property<string>("SerializedBookmarkPayload").CurrentValue;
        var inputJson = dbContext.Entry(entity).Property<string>("SerializedInput").CurrentValue;
        var affectedWorkflowInstancesIdsJson = dbContext.Entry(entity).Property<string>("SerializedAffectedWorkflowInstancesIds").CurrentValue;
        
        entity.BookmarkPayload = _payloadSerializer.Deserialize(bookmarkPayloadJson);
        entity.Input = !string.IsNullOrEmpty(inputJson) ? _payloadSerializer.Deserialize<Dictionary<string, object>>(inputJson) : null;
        entity.AffectedWorkflowInstancesIds = !string.IsNullOrEmpty(affectedWorkflowInstancesIdsJson) ? JsonSerializer.Deserialize<List<string>>(affectedWorkflowInstancesIdsJson) : null;

        return new(entity);
    }
}