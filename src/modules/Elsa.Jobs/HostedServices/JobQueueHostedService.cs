using System;
using System.Collections.Generic;
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
    private readonly int _workerCount;
    private readonly ChannelReader<IJob> _channelReader;
    private readonly IJobRunner _jobRunner;
    private readonly IList<Channel<IJob>> _outputs;
    private readonly ILogger _logger;

    public JobQueueHostedService(int workerCount, ChannelReader<IJob> channelReader, IJobRunner jobRunner, ILogger<JobQueueHostedService> logger)
    {
        _workerCount = workerCount;
        _channelReader = channelReader;
        _jobRunner = jobRunner;
        _logger = logger;
        _outputs = new List<Channel<IJob>>(workerCount);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var index = 0;
        
        for (var i = 0; i < _workerCount; i++)
        {
            var output = Channel.CreateUnbounded<IJob>();
            _outputs.Add(output);
            _ = ReadOutputAsync(output, cancellationToken);
        }
        
        await foreach (var job in _channelReader.ReadAllAsync(cancellationToken))
        {
            var output = _outputs[index];
            await output.Writer.WriteAsync(job, cancellationToken);
            index = (index + 1) % _workerCount;
        }

        foreach (var output in _outputs)
        {
            output.Writer.Complete();
        }
    }
    
    private async Task ReadOutputAsync(Channel<IJob> output, CancellationToken cancellationToken)
    {
        await foreach (var job in output.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await _jobRunner.RunJobAsync(job, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unhandled exception occured while processing the job queue");
            }        
        }
    }
}