using Elsa.Common.Contracts;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Contracts;
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
    public async ValueTask SaveAsync(string id, WorkflowState state, CancellationToken cancellationToken = default) => 
        await _store.SaveAsync(state, SaveAsync, cancellationToken: cancellationToken);

    /// <inheritdoc />
    public async ValueTask<WorkflowState?> LoadAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.WorkflowStates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
            return null;

        var entry = dbContext.Entry(entity);
        var json = entry.Property<string>("Data").CurrentValue;
        var state = await _workflowStateSerializer.DeserializeAsync(json, cancellationToken);

        return state;
    }

    /// <inheritdoc />
    public async ValueTask<int> CountAsync(CountRunningWorkflowsArgs args, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = dbContext.WorkflowStates.AsQueryable().Where(x => x.Status == WorkflowStatus.Running);

        if (args.DefinitionId != null) query = query.Where(x => x.DefinitionId == args.DefinitionId);
        if (args.Version != null) query = query.Where(x => x.DefinitionVersion == args.Version);
        if (args.CorrelationId != null) query = query.Where(x => x.CorrelationId == args.CorrelationId);

        return await query.CountAsync(cancellationToken);
    }
    
    private async ValueTask<WorkflowState> SaveAsync(RuntimeElsaDbContext dbContext, WorkflowState entity, CancellationToken cancellationToken)
    {
        var json = await _workflowStateSerializer.SerializeAsync(entity, cancellationToken);
        var now = _systemClock.UtcNow;
        var entry = dbContext.Entry(entity);

        if (entry.Property<DateTimeOffset>("CreatedAt").CurrentValue == DateTimeOffset.MinValue)
            entry.Property<DateTimeOffset>("CreatedAt").CurrentValue = now;

        entry.Property<string>("Data").CurrentValue = json;
        entry.Property<DateTimeOffset>("UpdatedAt").CurrentValue = now;
        return entity;
    }
}