using Elsa.Common.Contracts;
using Elsa.MongoDB.Common;
using Elsa.MongoDB.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Contracts;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDB.Stores.Runtime;

/// <inheritdoc />
public class MongoWorkflowStateStore : IWorkflowStateStore
{
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private readonly MongoStore<Models.WorkflowState> _mongoStore;
    private readonly ISystemClock _systemClock;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MongoWorkflowStateStore(
        MongoStore<Models.WorkflowState> mongoStore,
        IWorkflowStateSerializer workflowStateSerializer,
        ISystemClock systemClock)
    {
        _workflowStateSerializer = workflowStateSerializer;
        _mongoStore = mongoStore;
        _systemClock = systemClock;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(string id, WorkflowState state, CancellationToken cancellationToken = default)
    {
        var now = _systemClock.UtcNow;
        
        var currentState = await _mongoStore.FindAsync(x => x.Id == id, cancellationToken);
        var document = state.MapToDocument(_workflowStateSerializer);
        
        document.CreatedAt = currentState is null ? now : currentState.CreatedAt == DateTimeOffset.MinValue ? now : currentState.CreatedAt;
        document.UpdatedAt = now;

        await _mongoStore.SaveAsync(document, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<WorkflowState?> LoadAsync(string id, CancellationToken cancellationToken = default) => 
        (await _mongoStore.FindAsync(x => x.Id == id, cancellationToken))?.MapFromDocument(_workflowStateSerializer);

    /// <inheritdoc />
    public async ValueTask<int> CountAsync(CountRunningWorkflowsArgs args, CancellationToken cancellationToken = default)
    {
        var query = _mongoStore.GetCollection().AsQueryable().Where(x => x.Status == WorkflowStatus.Running);

        if (args.DefinitionId != null) query = query.Where(x => x.DefinitionId == args.DefinitionId);
        if (args.Version != null) query = query.Where(x => x.DefinitionVersion == args.Version);
        if (args.CorrelationId != null) query = query.Where(x => x.CorrelationId == args.CorrelationId);

        return (int)await query.LongCountAsync(cancellationToken);
    }
}