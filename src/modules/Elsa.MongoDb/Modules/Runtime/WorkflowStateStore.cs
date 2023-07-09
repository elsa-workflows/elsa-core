using Elsa.Common.Contracts;
using Elsa.MongoDb.Common;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Contracts;
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
    public async ValueTask<WorkflowState?> LoadAsync(string id, CancellationToken cancellationToken = default) => 
        (await _mongoDbStore.FindAsync(x => x.Id == id, cancellationToken));

    /// <inheritdoc />
    public async ValueTask<long> CountAsync(CountRunningWorkflowsArgs args, CancellationToken cancellationToken = default)
    {
        var query = _mongoDbStore.GetCollection().AsQueryable().Where(x => x.Status == WorkflowStatus.Running);

        if (args.DefinitionId != null) query = query.Where(x => x.DefinitionId == args.DefinitionId);
        if (args.Version != null) query = query.Where(x => x.DefinitionVersion == args.Version);
        if (args.CorrelationId != null) query = query.Where(x => x.CorrelationId == args.CorrelationId);

        return (int)await query.LongCountAsync(cancellationToken);
    }
}