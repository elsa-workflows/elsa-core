using System.Text.Json;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Filters;
using Elsa.Alterations.Core.Models;
using Elsa.EntityFrameworkCore.Common;
using Open.Linq.AsyncExtensions;

namespace Elsa.EntityFrameworkCore.Modules.Alterations;

/// <summary>
/// An EF Core implementation of <see cref="IAlterationPlanStore"/>.
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
        await _store.SaveAsync(record, OnSaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<AlterationJob> jobs, CancellationToken cancellationToken = default)
    {
        await _store.SaveManyAsync(jobs, OnSaveAsync, cancellationToken);
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
    public async Task<long> CountAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.CountAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    private static ValueTask OnSaveAsync(AlterationsElsaDbContext elsaDbContext, AlterationJob entity, CancellationToken cancellationToken)
    {
        elsaDbContext.Entry(entity).Property("SerializedLog").CurrentValue = JsonSerializer.Serialize(entity.Log);
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
}