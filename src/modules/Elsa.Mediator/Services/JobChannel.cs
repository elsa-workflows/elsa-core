using System.Threading.Channels;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;

namespace Elsa.Mediator.Services;

/// <inheritdoc />
public class JobChannel : IJobChannel
{
    private readonly Channel<EnqueuedJob> _channel;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobChannel"/> class.
    /// </summary>
    public JobChannel()
    {
        _channel = Channel.CreateUnbounded<EnqueuedJob>();
    }

    /// <inheritdoc />
    public ChannelWriter<EnqueuedJob> Writer => _channel.Writer;

    /// <inheritdoc />
    public ChannelReader<EnqueuedJob> Reader => _channel.Reader;
}