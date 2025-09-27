using Elsa.Alterations.Core.Results;

namespace Elsa.Alterations.Core.Contracts;

/// <summary>
/// Schedules a workflow instance to be resumed when an alteration job has been completed.
/// </summary>
public interface IAlteredWorkflowDispatcher
{
    /// <summary>
    /// Schedules the altered workflow instances to be resumed.
    /// </summary>
    /// <param name="results">The results of running a series of alterations.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task DispatchAsync(IEnumerable<RunAlterationsResult> results, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Schedules the altered workflow instance to be resumed.
    /// </summary>
    /// <param name="result">The result of running a series of alterations.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task DispatchAsync(RunAlterationsResult result, CancellationToken cancellationToken = default);
}