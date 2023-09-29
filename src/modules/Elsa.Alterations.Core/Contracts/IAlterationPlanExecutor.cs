using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Models;
using Elsa.Alterations.Core.Results;

namespace Elsa.Alterations.Core.Contracts;

/// <summary>
/// Executes an alteration plan.
/// </summary>
public interface IAlterationPlanExecutor
{
    /// <summary>
    /// Executes the specified alteration plan.
    /// </summary>
    /// <param name="plan">The alteration plan to execute.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The result of the execution.</returns>
    Task<AlterationPlanExecutionResult> ExecuteAsync(AlterationPlan plan, CancellationToken cancellationToken = default);
}