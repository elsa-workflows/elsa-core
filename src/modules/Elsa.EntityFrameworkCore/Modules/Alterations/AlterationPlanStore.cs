using System.Text.Json;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Filters;
using Elsa.EntityFrameworkCore.Common;

namespace Elsa.EntityFrameworkCore.Modules.Alterations;

/// <summary>
/// An EF Core implementation of <see cref="IAlterationPlanStore"/>.
/// </summary>
public class EFCoreAlterationPlanStore : IAlterationPlanStore
{
    private readonly EntityStore<AlterationsElsaDbContext, AlterationPlan> _store;
    private readonly IAlterationSerializer _alterationSerializer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreAlterationPlanStore(EntityStore<AlterationsElsaDbContext, AlterationPlan> store, IAlterationSerializer alterationSerializer)
    {
        _store = store;
        _alterationSerializer = alterationSerializer;
    }

    /// <inheritdoc />
    public async Task SaveAsync(AlterationPlan record, CancellationToken cancellationToken = default)
    {
        await _store.SaveAsync(record, OnSaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AlterationPlan?> FindAsync(AlterationPlanFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.FindAsync(filter.Apply, OnLoadAsync, cancellationToken);
    }

    public async Task<long> CountAsync(AlterationPlanFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.CountAsync(filter.Apply, cancellationToken);
    }

    private ValueTask OnSaveAsync(AlterationsElsaDbContext elsaDbContext, AlterationPlan entity, CancellationToken cancellationToken)
    {
        elsaDbContext.Entry(entity).Property("SerializedAlterations").CurrentValue = _alterationSerializer.SerializeMany(entity.Alterations);
        elsaDbContext.Entry(entity).Property("SerializedWorkflowInstanceIds").CurrentValue = JsonSerializer.Serialize(entity.WorkflowInstanceIds);
        return default;
    }

    private ValueTask OnLoadAsync(AlterationsElsaDbContext elsaDbContext, AlterationPlan? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return default;

        var alterationsJson = elsaDbContext.Entry(entity).Property<string>("SerializedAlterations").CurrentValue;
        var workflowInstanceIdsJson = elsaDbContext.Entry(entity).Property<string>("SerializedWorkflowInstanceIds").CurrentValue;
        entity.Alterations = _alterationSerializer.DeserializeMany(alterationsJson).ToList();
        entity.WorkflowInstanceIds = JsonSerializer.Deserialize<string[]>(workflowInstanceIdsJson)!;

        return default;
    }
}