using Elsa.Common.Contracts;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.MongoDb.Common;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDb.Modules.Runtime;

/// <summary>
/// A MongoDb implementation of <see cref="IBookmarkStore"/>.
/// </summary>
public class MongoWorkflowInboxMessageStore : IWorkflowInboxMessageStore
{
    private readonly MongoDbStore<WorkflowInboxMessage> _mongoDbStore;
    private readonly ISystemClock _systemClock;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoWorkflowInboxMessageStore"/> class.
    /// </summary>
    public MongoWorkflowInboxMessageStore(MongoDbStore<WorkflowInboxMessage> mongoDbStore, ISystemClock systemClock)
    {
        _mongoDbStore = mongoDbStore;
        _systemClock = systemClock;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(WorkflowInboxMessage record, CancellationToken cancellationToken = default) =>
        await _mongoDbStore.SaveAsync(record, s => s.Id, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(WorkflowInboxMessageFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.FindManyAsync(query => Filter(query, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(IEnumerable<WorkflowInboxMessageFilter> filters, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.FindManyAsync(query => Filter(query, filters.ToArray()), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteManyAsync(WorkflowInboxMessageFilter filter, PageArgs? pageArgs = default, CancellationToken cancellationToken = default) =>
        await _mongoDbStore.DeleteWhereAsync<string>(query => Paginate(Filter(query, filter), pageArgs), x => x.Id, cancellationToken);

    private IMongoQueryable<WorkflowInboxMessage> Filter(IMongoQueryable<WorkflowInboxMessage> queryable, params WorkflowInboxMessageFilter[] filters)
    {
        foreach (var filter in filters) filter.Apply(queryable, _systemClock.UtcNow);
        return queryable;
    }

    private IMongoQueryable<WorkflowInboxMessage> Paginate(IMongoQueryable<WorkflowInboxMessage> queryable, PageArgs? pageArgs) => (queryable.Paginate(pageArgs) as IMongoQueryable<WorkflowInboxMessage>)!;
}