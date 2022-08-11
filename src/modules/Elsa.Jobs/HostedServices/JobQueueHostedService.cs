using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Elsa.Jobs.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Jobs.HostedServices;

/// <summary>
/// Continuously reads from a channel to which jobs can be sent, executing each received job.
/// </summary>
public class JobQueueHostedService : BackgroundService
{
    private readonly ChannelReader<IJob> _channelReader;
    private readonly IJobRunner _jobRunner;
    private readonly ILogger _logger;

    public JobQueueHostedService(ChannelReader<IJob> channelReader, IJobRunner jobRunner, ILogger<JobQueueHostedService> logger)
    {
        _channelReader = channelReader;
        _jobRunner = jobRunner;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await foreach (var job in _channelReader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await _jobRunner.RunJobAsync(job, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unhandled exception occured while running a job");
            }
        }
    }
}