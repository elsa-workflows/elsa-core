namespace Elsa.Jobs.Services;

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