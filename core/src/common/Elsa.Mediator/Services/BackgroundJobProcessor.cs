using Elsa.Mediator.Contracts;
using Elsa.Mediator.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Mediator.Services;

/// <summary>
/// Continuously reads from a channel to which jobs can be sent, executing each received job.
/// </summary>
public class BackgroundJobProcessor
{
    private readonly int _workerCount;
    private readonly IJobsChannel _jobsChannel;
    private readonly ILogger<BackgroundJobProcessor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundJobProcessor"/> class.
    /// </summary>
    public BackgroundJobProcessor(IOptions<MediatorOptions> options, IJobsChannel jobsChannel, ILogger<BackgroundJobProcessor> logger)
    {
        _workerCount = options.Value.JobWorkerCount;
        _jobsChannel = jobsChannel;
        _logger = logger;
    }

    /// <summary>
    /// Runs the processor until cancellation is requested.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var workers = new Task[_workerCount];

        for (var i = 0; i < _workerCount; i++)
            workers[i] = ProcessJobsAsync(cancellationToken);

        await Task.WhenAll(workers);
    }

    private async Task ProcessJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var jobItem in _jobsChannel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, jobItem.CancellationTokenSource.Token);
                    await jobItem.Action(linkedTokenSource.Token);
                    _logger.LogInformation("Worker {CurrentTaskId} processed job {JobId}", Task.CurrentId, jobItem.JobId);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Job {JobId} was canceled", jobItem.JobId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Job {JobId} failed", jobItem.JobId);
                }
                finally
                {
                    jobItem.OnJobCompleted(jobItem.JobId);
                }
            }
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "An operation was cancelled while processing the job queue");
        }
    }
}
