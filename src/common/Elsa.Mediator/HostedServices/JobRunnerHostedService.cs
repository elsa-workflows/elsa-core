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
        var workers = new Task[_workerCount];

        for (var i = 0; i < _workerCount; i++)
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