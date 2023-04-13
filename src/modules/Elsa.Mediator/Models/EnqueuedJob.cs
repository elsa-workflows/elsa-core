namespace Elsa.Mediator.Models;

/// <summary>
/// Represents an item in the job queue.
/// </summary>
/// <param name="JobId">The ID of the job.</param>
/// <param name="Action">The job to execute.</param>
/// <param name="CancellationTokenSource">The cancellation token source.</param>
/// <param name="OnJobCompleted">The action to invoke when the job has completed.</param>
public record EnqueuedJob(string JobId, Func<CancellationToken, Task> Action, CancellationTokenSource CancellationTokenSource, Action<string> OnJobCompleted);