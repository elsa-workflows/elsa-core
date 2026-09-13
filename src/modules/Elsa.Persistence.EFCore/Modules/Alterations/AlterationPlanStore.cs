using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Filters;
using Elsa.Alterations.Core.Models;
using Elsa.Alterations.Core.Stores;
using Elsa.Tenants.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Elsa.Persistence.EFCore.Modules.Alterations;

/// <summary>
/// An EF Core implementation of <see cref="IAlterationPlanStore"/>.
/// </summary>
public class EFCoreAlterationPlanStore : IAlterationPlanStore
{
    private readonly EntityStore<AlterationsElsaDbContext, AlterationPlan> _store;
    private readonly IAlterationSerializer _alterationSerializer;
    private readonly bool _tenantEnabled;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreAlterationPlanStore(
        EntityStore<AlterationsElsaDbContext, AlterationPlan> store,
        IAlterationSerializer alterationSerializer,
        IOptions<TenantsOptions> tenantsOptions)
    {
        _store = store;
        _alterationSerializer = alterationSerializer;
        _tenantEnabled = tenantsOptions.Value.IsEnabled;
    }

    /// <inheritdoc />
    public async Task SaveAsync(AlterationPlan record, CancellationToken cancellationToken = default)
    {
        if (!_tenantEnabled)
        {
            await _store.SaveAsync(record, OnSaveAsync, cancellationToken);
            return;
        }

        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var ambientTenantId = AlterationTenantOwnedUpsert.AmbientTenantId(dbContext);
        AlterationTenantOwnedUpsert.StampTenantId(record, ambientTenantId);
        OnSave(dbContext, record);

        var status = record.Status;
        var createdAt = record.CreatedAt;
        var startedAt = record.StartedAt;
        var completedAt = record.CompletedAt;
        var serializedAlterations = dbContext.Entry(record).Property<string>("SerializedAlterations").CurrentValue;
        var serializedFilter = dbContext.Entry(record).Property<string>("SerializedWorkflowInstanceFilter").CurrentValue;

        var query = dbContext.Set<AlterationPlan>()
            .IgnoreQueryFilters()
            .Where(AlterationTenantOwnedUpsert.OwnedId<AlterationPlan>(record.Id, record.TenantId, ambientTenantId));

        // Inline lambda so net8/net9 bind SetPropertyCalls and net10 binds UpdateSettersBuilder.
        Task<int> UpdateOwnedAsync() => query.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(plan => plan.Status, status)
                .SetProperty(plan => plan.CreatedAt, createdAt)
                .SetProperty(plan => plan.StartedAt, startedAt)
                .SetProperty(plan => plan.CompletedAt, completedAt)
                .SetProperty(plan => EF.Property<string>(plan, "SerializedAlterations"), serializedAlterations)
                .SetProperty(plan => EF.Property<string>(plan, "SerializedWorkflowInstanceFilter"), serializedFilter),
            cancellationToken);

        var updated = await UpdateOwnedAsync();

        if (updated == 0)
        {
            var inserted = await AlterationTenantOwnedUpsert.InsertIfAbsentAsync(dbContext, record, cancellationToken);
            if (!inserted)
            {
                var retried = await UpdateOwnedAsync();

                if (retried == 0)
                    throw AlterationStoreConflict.HiddenPlanId(record.Id);
            }
        }
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    public async Task<AlterationPlan?> FindAsync(AlterationPlanFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.FindAsync(filter.Apply, OnLoadAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(AlterationPlanFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.CountAsync(filter.Apply, cancellationToken);
    }

    private void OnSave(AlterationsElsaDbContext elsaDbContext, AlterationPlan entity)
    {
        elsaDbContext.Entry(entity).Property("SerializedAlterations").CurrentValue = _alterationSerializer.SerializeMany(entity.Alterations);
        elsaDbContext.Entry(entity).Property("SerializedWorkflowInstanceFilter").CurrentValue = JsonSerializer.Serialize(entity.WorkflowInstanceFilter);
    }

    private ValueTask OnSaveAsync(AlterationsElsaDbContext dbContext, AlterationPlan entity, CancellationToken cancellationToken)
    {
        OnSave(dbContext, entity);
        return default;
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    private ValueTask OnLoadAsync(AlterationsElsaDbContext elsaDbContext, AlterationPlan? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return default;

        var alterationsJson = elsaDbContext.Entry(entity).Property<string>("SerializedAlterations").CurrentValue;
        var workflowInstanceFilterJson = elsaDbContext.Entry(entity).Property<string>("SerializedWorkflowInstanceFilter").CurrentValue;
        entity.Alterations = _alterationSerializer.DeserializeMany(alterationsJson).ToList();
        entity.WorkflowInstanceFilter = JsonSerializer.Deserialize<AlterationWorkflowInstanceFilter>(workflowInstanceFilterJson)!;

        return default;
    }
}
