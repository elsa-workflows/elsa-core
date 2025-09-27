using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Filters;
using Elsa.Common.Services;

namespace Elsa.Alterations.Core.Stores;

/// <summary>
/// A memory-based store for alteration plans.
/// </summary>
public class MemoryAlterationPlanStore : IAlterationPlanStore
{
    private readonly MemoryStore<AlterationPlan> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryAlterationPlanStore"/> class.
    /// </summary>
    public MemoryAlterationPlanStore(MemoryStore<AlterationPlan> store)
    {
        _store = store;
    }
    
    /// <inheritdoc />
    public Task SaveAsync(AlterationPlan plan, CancellationToken cancellationToken = default)
    {
        _store.Save(plan, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<AlterationPlan?> FindAsync(AlterationPlanFilter filter, CancellationToken cancellationToken = default)
    {
        var entity = _store.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task<long> CountAsync(AlterationPlanFilter filter, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter)).LongCount();
        return Task.FromResult(count);
    }

    private static IQueryable<AlterationPlan> Filter(IQueryable<AlterationPlan> query, AlterationPlanFilter filter) => filter.Apply(query);
}