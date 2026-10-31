using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Filters;

namespace Elsa.Alterations.Core.Contracts;

/// <summary>
/// A store for alteration plans.
/// </summary>
public interface IAlterationPlanStore
{
    /// <summary>
    /// Saves the specified alteration plan.
    /// </summary>
    Task SaveAsync(AlterationPlan plan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the alteration plan matching the specified filter.
    /// </summary>
    Task<AlterationPlan?> FindAsync(AlterationPlanFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the number of alteration plans matching the specified filter.
    /// </summary>
    Task<long> CountAsync(AlterationPlanFilter filter, CancellationToken cancellationToken = default);
}