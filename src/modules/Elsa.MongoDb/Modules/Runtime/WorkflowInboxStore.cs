using Elsa.MongoDb.Common;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDb.Modules.Runtime;

/// <summary>
/// A MongoDb implementation of <see cref="IBookmarkStore"/>.
/// </summary>
public class MongoWorkflowInboxStore : IWorkflowInboxStore
{
    private readonly MongoDbStore<WorkflowInboxMessage> _mongoDbStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoWorkflowInboxStore"/> class.
    /// </summary>
    public MongoWorkflowInboxStore(MongoDbStore<WorkflowInboxMessage> mongoDbStore)
    {
        _mongoDbStore = mongoDbStore;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(WorkflowInboxMessage record, CancellationToken cancellationToken = default) => 
        await _mongoDbStore.SaveAsync(record, s => s.Id, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(WorkflowInboxMessageFilter filter, CancellationToken cancellationToken = default) => 
        (await _mongoDbStore.FindManyAsync(query => Filter(query, filter), cancellationToken));

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(WorkflowInboxMessageFilter filter, CancellationToken cancellationToken = default) => 
        await _mongoDbStore.DeleteWhereAsync<string>(query => Filter(query, filter), x => x.Id, cancellationToken);

    private IMongoQueryable<WorkflowInboxMessage> Filter(IMongoQueryable<WorkflowInboxMessage> queryable, WorkflowInboxMessageFilter filter) => 
        (filter.Apply(queryable) as IMongoQueryable<WorkflowInboxMessage>)!;
}