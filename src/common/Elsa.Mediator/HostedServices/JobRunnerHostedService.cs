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
    private readonly IJobsChannel _jobsChannel;
    private readonly ILogger<JobRunnerHostedService> _logger;

    /// <inheritdoc />
    public JobRunnerHostedService(IOptions<MediatorOptions> options, IJobsChannel jobsChannel, ILogger<JobRunnerHostedService> logger)
    {
        _jobsChannel = jobsChannel;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessJobsAsync(stoppingToken);
        await ProcessJobsAsync(stoppingToken);
    }

    private async Task ProcessJobsAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobItem in _jobsChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
               _logger.LogInformation(
                   "Worker {CurrentTaskId} about to process job {JobId}",
                   Task.CurrentId,
                   jobItem.JobId
               );
                 var task = jobItem
                  .Action(jobItem.CancellationTokenSource.Token)
                  .ContinueWith(
                   async (theTask) =>
                    {
                        try
                        {
                            await theTask.ConfigureAwait(false);
                            _logger.LogInformation(
                                "Worker {CurrentTaskId} processed job {JobId}",
                                Task.CurrentId,
                                jobItem.JobId
                            );
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
                );
        }
    }
}
