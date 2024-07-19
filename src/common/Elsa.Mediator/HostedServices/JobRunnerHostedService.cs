using Elsa.Mediator.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.HostedServices;

/// <summary>
/// A hosted service that runs jobs.
/// </summary>
/// <inheritdoc />
public class JobRunnerHostedService(IJobsChannel jobsChannel, ILogger<JobRunnerHostedService> logger) : BackgroundService
{
    private const int WorkerCount = 4;
    private readonly IJobsChannel _jobsChannel = jobsChannel;
    private readonly ILogger<JobRunnerHostedService> _logger = logger;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workers = new Task[WorkerCount];
        
        for (var i = 0; i < WorkerCount; i++) 
            workers[i] = ProcessJobsAsync(stoppingToken);

        await Task.WhenAll(workers);
    }

    private async Task ProcessJobsAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobItem in _jobsChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await jobItem.Action(jobItem.CancellationTokenSource.Token);
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
}