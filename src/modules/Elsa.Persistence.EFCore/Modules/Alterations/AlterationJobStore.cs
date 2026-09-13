using System.Data.Common;
using System.Text.Json;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Filters;
using Elsa.Alterations.Core.Models;
using Elsa.Alterations.Core.Stores;
using Elsa.Tenants.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Open.Linq.AsyncExtensions;

namespace Elsa.Persistence.EFCore.Modules.Alterations;

/// <summary>
/// An EF Core implementation of <see cref="IAlterationJobStore"/>.
/// </summary>
public class EFCoreAlterationJobStore : IAlterationJobStore
{
    private readonly EntityStore<AlterationsElsaDbContext, AlterationJob> _store;
    private readonly bool _tenantEnabled;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreAlterationJobStore(EntityStore<AlterationsElsaDbContext, AlterationJob> store)
        : this(store, Options.Create(new TenantsOptions()))
    {
    }

    /// <summary>
    /// Constructor used by dependency injection. Direct construction through the legacy
    /// overload keeps tenancy-aware upsert disabled for compatibility.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public EFCoreAlterationJobStore(EntityStore<AlterationsElsaDbContext, AlterationJob> store, IOptions<TenantsOptions> tenantsOptions)
    {
        _store = store;
        _tenantEnabled = tenantsOptions.Value.IsEnabled;
    }

    /// <inheritdoc />
    public async Task SaveAsync(AlterationJob record, CancellationToken cancellationToken = default)
    {
        if (!_tenantEnabled)
        {
            await _store.SaveAsync(record, OnSaveAsync, cancellationToken);
            return;
        }

        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        await UpsertAsync(dbContext, record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<AlterationJob> jobs, CancellationToken cancellationToken = default)
    {
        if (!_tenantEnabled)
        {
            await _store.SaveManyAsync(jobs, OnSaveAsync, cancellationToken);
            return;
        }

        var list = jobs.OrderBy(job => job.Id, StringComparer.Ordinal).ToList();
        if (list.Count == 0)
            return;

        await _store.ExecuteWithDbExceptionHandlingAsync(
            async () =>
            {
                await _store.ExecuteSqlServerWriteWithRetryAsync(async (dbContext, ct) =>
                {
                    await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

                    foreach (var job in list)
                        await UpsertAsync(dbContext, job, ct, handleDbExceptions: false);

                    await transaction.CommitAsync(ct);
                }, cancellationToken);
                return true;
            },
            cancellationToken,
            IsDatabaseException);
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

    private async Task UpsertAsync(
        AlterationsElsaDbContext dbContext,
        AlterationJob record,
        CancellationToken cancellationToken,
        bool handleDbExceptions = true)
    {
        var ambientTenantId = AlterationTenantOwnedUpsert.AmbientTenantId(dbContext);
        AlterationTenantOwnedUpsert.StampTenantId(record, ambientTenantId);
        OnSave(dbContext, record);

        var planId = record.PlanId;
        var workflowInstanceId = record.WorkflowInstanceId;
        var status = record.Status;
        var createdAt = record.CreatedAt;
        var startedAt = record.StartedAt;
        var completedAt = record.CompletedAt;
        var serializedLog = dbContext.Entry(record).Property<string>("SerializedLog").CurrentValue;

        var query = dbContext.Set<AlterationJob>()
            .IgnoreQueryFilters()
            .Where(AlterationTenantOwnedUpsert.OwnedId<AlterationJob>(record.Id, record.TenantId, ambientTenantId));

        // Inline lambda so net8/net9 bind SetPropertyCalls and net10 binds UpdateSettersBuilder.
        Task<int> UpdateOwnedAsync() => query.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(job => job.PlanId, planId)
                .SetProperty(job => job.WorkflowInstanceId, workflowInstanceId)
                .SetProperty(job => job.Status, status)
                .SetProperty(job => job.CreatedAt, createdAt)
                .SetProperty(job => job.StartedAt, startedAt)
                .SetProperty(job => job.CompletedAt, completedAt)
                .SetProperty(job => EF.Property<string>(job, "SerializedLog"), serializedLog),
            cancellationToken);

        Task<TResult> ExecuteWriteAsync<TResult>(Func<Task<TResult>> operation) =>
            handleDbExceptions
                ? _store.ExecuteWithDbExceptionHandlingAsync(operation, cancellationToken)
                : operation();

        var updated = await ExecuteWriteAsync(UpdateOwnedAsync);

        if (updated == 0)
        {
            var inserted = await ExecuteWriteAsync(
                () => AlterationTenantOwnedUpsert.InsertIfAbsentAsync(dbContext, record, cancellationToken));
            if (!inserted)
            {
                var retried = await ExecuteWriteAsync(UpdateOwnedAsync);

                if (retried == 0)
                    throw AlterationStoreConflict.HiddenJobId(record.Id);
            }
        }
    }

    private static void OnSave(AlterationsElsaDbContext elsaDbContext, AlterationJob entity)
    {
        elsaDbContext.Entry(entity).Property("SerializedLog").CurrentValue = JsonSerializer.Serialize(entity.Log);
    }

    private static ValueTask OnSaveAsync(AlterationsElsaDbContext dbContext, AlterationJob entity, CancellationToken cancellationToken)
    {
        OnSave(dbContext, entity);
        return default;
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

    private static bool IsDatabaseException(Exception exception) =>
        exception is DbException or DbUpdateException || exception.InnerException is DbException;
}
