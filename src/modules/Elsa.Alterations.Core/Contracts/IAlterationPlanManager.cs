using Elsa.Alterations.Core.Entities;

namespace Elsa.Alterations.Core.Contracts;

/// <summary>
/// Represents a manager for alteration plans.
/// </summary>
public interface IAlterationPlanManager
{
    /// <summary>
    /// Gets an alteration plan by ID.
    /// </summary>
    Task<AlterationPlan?> GetPlanAsync(string planId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a value indicating whether all jobs in the plan have been completed.
    /// </summary>
    Task<bool> GetIsAllJobsCompletedAsync(string planId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Completes an alteration plan.
    /// </summary>
    Task CompletePlanAsync(AlterationPlan plan, CancellationToken cancellationToken = default);
}