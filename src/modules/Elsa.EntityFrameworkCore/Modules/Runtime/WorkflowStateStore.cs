using Elsa.Common.Contracts;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <inheritdoc />
public class EFCoreWorkflowStateStore : IWorkflowStateStore
{
    private readonly ISystemClock _systemClock;
    private readonly IDbContextFactory<RuntimeElsaDbContext> _dbContextFactory;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private readonly EntityStore<RuntimeElsaDbContext, WorkflowState> _store;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreWorkflowStateStore(
        EntityStore<RuntimeElsaDbContext, WorkflowState> store,
        IDbContextFactory<RuntimeElsaDbContext> dbContextFactory,
        IWorkflowStateSerializer workflowStateSerializer,
        ISystemClock systemClock)
    {
        _systemClock = systemClock;
        _dbContextFactory = dbContextFactory;
        _workflowStateSerializer = workflowStateSerializer;
        _store = store;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(string id, WorkflowState state, CancellationToken cancellationToken = default)
    {
        await _store.SaveAsync(state, SaveAsync, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<WorkflowState?> FindAsync(WorkflowStateFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.FindAsync(filter.Apply, LoadAsync, cancellationToken);
    }
    
    /// <inheritdoc />
    public async ValueTask<long> CountAsync(WorkflowStateFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.CountAsync(filter.Apply, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(WorkflowStateFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.DeleteWhereAsync(filter.Apply, cancellationToken);
    }

    private async ValueTask<WorkflowState> SaveAsync(RuntimeElsaDbContext dbContext, WorkflowState entity, CancellationToken cancellationToken)
    {
        var json = await _workflowStateSerializer.SerializeAsync(entity, cancellationToken);
        var now = _systemClock.UtcNow;
        var entry = dbContext.Entry(entity);

        entity.CreatedAt = entity.CreatedAt == default ? now : entity.CreatedAt;
        entity.UpdatedAt = now;

        entry.Property<string>("Data").CurrentValue = json;
        return entity;
    }
    
    private async ValueTask<WorkflowState?> LoadAsync(RuntimeElsaDbContext dbContext, WorkflowState? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return entity;

        var entry = dbContext.Entry(entity);
        var json = entry.Property<string>("Data").CurrentValue;
        var state = await _workflowStateSerializer.DeserializeAsync(json, cancellationToken);

        return state;
    }

}