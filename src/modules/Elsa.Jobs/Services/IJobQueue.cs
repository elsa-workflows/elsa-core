namespace Elsa.Jobs.Services;

/// <summary>
/// Represents a job queue to which jobs can be submitted.
/// </summary>
public interface IJobQueue
{
    Task SubmitJobAsync(IJob job, string? queueName = default, CancellationToken cancellationToken = default);
}