using Elsa.Alterations.Core.Models;

namespace Elsa.Alterations.Core.Contracts;

/// <summary>
/// Scheduler for alteration plans.
/// </summary>
public interface IAlterationPlanScheduler
{
    /// <summary>
    /// Submits an alteration plan for execution.
    /// </summary>
    Task<string> SubmitAsync(NewAlterationPlan newPlan, CancellationToken cancellationToken = default);
}