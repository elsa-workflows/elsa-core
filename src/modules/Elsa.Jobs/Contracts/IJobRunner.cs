namespace Elsa.Jobs.Contracts;

/// <summary>
/// Runs specified jobs.
/// </summary>
public interface IJobRunner
{
    /// <summary>
    /// Runs the specified job.
    /// </summary>
    Task RunJobAsync(IJob job, CancellationToken cancellationToken = default);
}