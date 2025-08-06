using Elsa.Mediator.Contracts;
using Elsa.Mediator.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Mediator.HostedServices;

/// <summary>
/// A hosted service that runs jobs.
/// </summary>
public class JobRunnerHostedService : BackgroundService
{
    private readonly int _workerCount;
    private readonly IJobsChannel _jobsChannel;
    private readonly ILogger<JobRunnerHostedService> _logger;

    /// <inheritdoc />
    public JobRunnerHostedService(IOptions<MediatorOptions> options, IJobsChannel jobsChannel, ILogger<JobRunnerHostedService> logger)
    {
        _workerCount = options.Value.JobWorkerCount;
        _jobsChannel = jobsChannel;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Create an array of worker tasks to process jobs in parallel
        var workers = new Task[_workerCount];

        // Start multiple workers (tasks) to process jobs concurrently
        for (var i = 0; i < _workerCount; i++)
            workers[i] = ProcessJobsAsync(stoppingToken);

        // Wait for all worker tasks to complete (typically when the application is shutting down)
        await Task.WhenAll(workers);
    }

    /// <summary>
    /// Continuously processes jobs from the job channel until cancellation is requested.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token from the hosted service</param>
    private async Task ProcessJobsAsync(CancellationToken stoppingToken)
    {
        // Process all jobs from the channel until it's completed or cancellation is requested
        await foreach (var jobItem in _jobsChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                // Link the cancellation tokens so that cancellation can happen from either source
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, jobItem.CancellationTokenSource.Token);
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
                // Notify that the job has completed (whether successfully or not)
                jobItem.OnJobCompleted(jobItem.JobId);
            }
        }
    }
}