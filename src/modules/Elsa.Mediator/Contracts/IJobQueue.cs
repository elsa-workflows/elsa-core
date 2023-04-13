namespace Elsa.Mediator.Contracts;

/// <summary>
/// Manages jobs.
/// </summary>
public interface IJobQueue
{
    /// <summary>
    /// Posts a job.
    /// </summary>
    /// <param name="job">The job to post.</param>
    /// <returns>The ID of the job.</returns>
    string Enqueue(Func<CancellationToken, Task> job);
    
    /// <summary>
    /// Cancels a job.
    /// </summary>
    /// <param name="jobId">The ID of the job to cancel.</param>
    /// <returns><c>true</c> if the job was cancelled; otherwise, <c>false</c>.</returns>
    bool Cancel(string jobId);
}