using System.Text.Json;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Filters;
using Elsa.Alterations.Core.Models;
using Microsoft.EntityFrameworkCore;
using Open.Linq.AsyncExtensions;

namespace Elsa.Persistence.EFCore.Modules.Alterations;

/// <summary>
/// An EF Core implementation of <see cref="IAlterationJobStore"/>.
/// </summary>
public class EFCoreAlterationJobStore : IAlterationJobStore
{
    private readonly EntityStore<AlterationsElsaDbContext, AlterationJob> _store;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreAlterationJobStore(EntityStore<AlterationsElsaDbContext, AlterationJob> store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public async Task SaveAsync(AlterationJob record, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        await UpsertAsync(dbContext, record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<AlterationJob> jobs, CancellationToken cancellationToken = default)
    {
        var list = jobs.ToList();
        if (list.Count == 0)
            return;

        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var job in list)
            await UpsertAsync(dbContext, job, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AlterationJob?> FindAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.FindAsync(filter.Apply, OnLoadAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AlterationJob>> FindManyAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.QueryAsync(filter.Apply, OnLoadAsync, cancellationToken).ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> FindManyIdsAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.QueryAsync(filter.Apply, x => x.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.CountAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    private static async Task UpsertAsync(AlterationsElsaDbContext dbContext, AlterationJob record, CancellationToken cancellationToken)
    {
        var ambientTenantId = AlterationTenantOwnedUpsert.AmbientTenantId(dbContext);
        AlterationTenantOwnedUpsert.StampTenantId(record, ambientTenantId);
        OnSave(dbContext, record);

        var tenantId = record.TenantId;
        var planId = record.PlanId;
        var workflowInstanceId = record.WorkflowInstanceId;
        var status = record.Status;
        var createdAt = record.CreatedAt;
        var startedAt = record.StartedAt;
        var completedAt = record.CompletedAt;
        var serializedLog = dbContext.Entry(record).Property<string>("SerializedLog").CurrentValue;

        var updated = await dbContext.Set<AlterationJob>()
            .IgnoreQueryFilters()
            .Where(AlterationTenantOwnedUpsert.OwnedId<AlterationJob>(record.Id, record.TenantId, ambientTenantId))
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(job => job.TenantId, tenantId)
                    .SetProperty(job => job.PlanId, planId)
                    .SetProperty(job => job.WorkflowInstanceId, workflowInstanceId)
                    .SetProperty(job => job.Status, status)
                    .SetProperty(job => job.CreatedAt, createdAt)
                    .SetProperty(job => job.StartedAt, startedAt)
                    .SetProperty(job => job.CompletedAt, completedAt)
                    .SetProperty(job => EF.Property<string>(job, "SerializedLog"), serializedLog),
                cancellationToken);

        if (updated == 0)
            await AlterationTenantOwnedUpsert.InsertIfAbsentAsync(dbContext, record, isPlan: false, cancellationToken);
    }

    private static void OnSave(AlterationsElsaDbContext elsaDbContext, AlterationJob entity)
    {
        elsaDbContext.Entry(entity).Property("SerializedLog").CurrentValue = JsonSerializer.Serialize(entity.Log);
    }

    private static ValueTask OnLoadAsync(AlterationsElsaDbContext elsaDbContext, AlterationJob? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return default;

        var logJson = elsaDbContext.Entry(entity).Property<string>("SerializedLog").CurrentValue;
        entity.Log = JsonSerializer.Deserialize<AlterationLogEntry[]>(logJson)!;

        return default;
    }

    private static IQueryable<AlterationJob> Filter(IQueryable<AlterationJob> queryable, AlterationJobFilter filter) => filter.Apply(queryable);
}
