namespace Elsa.Alterations.Core.Contracts;

/// <summary>
///  Dispatches an alteration job for execution.
/// </summary>
public interface IAlterationJobDispatcher
{
    /// <summary>
    /// Dispatches an alteration job for execution.
    /// </summary>
    /// <param name="jobId">The ID of the job to dispatch.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    ValueTask DispatchAsync(string jobId, CancellationToken cancellationToken = default);
}