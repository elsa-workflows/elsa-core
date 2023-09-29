using Elsa.Alterations.Core.Results;

namespace Elsa.Alterations.Core.Contracts;

/// <summary>
/// Commits the result of an alteration plan execution.
/// </summary>
public interface IAlterationPlanResultCommitter
{
    /// <summary>
    /// Commits the result of an alteration plan execution.
    /// </summary>
    Task CommitAsync(AlterationPlanExecutionResult result, CancellationToken cancellationToken = default);
}