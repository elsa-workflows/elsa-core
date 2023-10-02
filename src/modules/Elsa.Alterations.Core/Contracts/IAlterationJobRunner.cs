using Elsa.Alterations.Core.Entities;

namespace Elsa.Alterations.Core.Contracts;

/// <summary>
/// Executes an alteration job.
/// </summary>
public interface IAlterationJobRunner
{
    /// <summary>
    /// Executes the specified alteration plan.
    /// </summary>
    /// <param name="jobId">The Id of the alteration job to execute.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The job with updated state.</returns>
    Task<AlterationJob> RunAsync(string jobId, CancellationToken cancellationToken = default);
}