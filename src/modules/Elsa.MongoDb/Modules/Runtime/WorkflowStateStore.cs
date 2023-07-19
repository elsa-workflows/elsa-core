using Elsa.Common.Contracts;
using Elsa.MongoDb.Common;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDb.Modules.Runtime;

/// <inheritdoc />
public class MongoWorkflowStateStore : IWorkflowStateStore
{
    private readonly MongoDbStore<WorkflowState> _mongoDbStore;
    private readonly ISystemClock _systemClock;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MongoWorkflowStateStore(
        MongoDbStore<WorkflowState> mongoDbStore,
        ISystemClock systemClock)
    {
        _mongoDbStore = mongoDbStore;
        _systemClock = systemClock;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(string id, WorkflowState state, CancellationToken cancellationToken = default)
    {
        var now = _systemClock.UtcNow;
        
        var currentState = await _mongoDbStore.FindAsync(x => x.Id == id, cancellationToken);

        state.CreatedAt = currentState is null || currentState.CreatedAt == default ? now : currentState.CreatedAt;
        state.UpdatedAt = now;

        await _mongoDbStore.SaveAsync(state, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<WorkflowState?> FindAsync(WorkflowStateFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.FindAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> CountAsync(WorkflowStateFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.CountAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(WorkflowStateFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.DeleteWhereAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    private IMongoQueryable<WorkflowState> Filter(IMongoQueryable<WorkflowState> queryable, WorkflowStateFilter filter) => (filter.Apply(queryable) as IMongoQueryable<WorkflowState>)!;
}