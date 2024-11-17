namespace Elsa.Mediator.Contracts;

/// <summary>
/// Manages jobs.
/// </summary>
public interface IJobQueue
{
    /// <summary>
    /// Creates a pending job. The caller must use the returned ID to schedule the job for execution.
    /// </summary>
    string Create(Func<CancellationToken, Task> job);
    
    /// <summary>
    /// Enqueues a job for execution.
    /// </summary>
    /// <param name="jobId">The ID of the job to enqueue.</param>
    void Enqueue(string jobId);
    
    /// <summary>
    /// Enqueues a job for execution.
    /// </summary>
    /// <param name="job">The job to enqueue.</param>
    /// <returns>The ID of the job.</returns>
    string Enqueue(Func<CancellationToken, Task> job);
    
    /// <summary>
    /// Cancels a job.
    /// </summary>
    /// <param name="jobId">The ID of the job to cancel.</param>
    /// <returns><c>true</c> if the job was cancelled; otherwise, <c>false</c>.</returns>
    bool Cancel(string jobId);
}