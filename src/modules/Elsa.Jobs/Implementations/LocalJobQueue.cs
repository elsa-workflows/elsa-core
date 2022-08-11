using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Elsa.Jobs.Services;

namespace Elsa.Jobs.Implementations;

/// <summary>
/// Represents a local, in-memory queue of jobs that will be processed in-process.
/// </summary>
public class LocalJobQueue : IJobQueue
{
    private readonly ChannelWriter<IJob> _channelWriter;

    public LocalJobQueue(ChannelWriter<IJob> channelWriter)
    {
        _channelWriter = channelWriter;
    }
    
    public async Task SubmitJobAsync(IJob job, string? queueName = default, CancellationToken cancellationToken = default)
    {
        await _channelWriter.WriteAsync(job, cancellationToken);
    }
}