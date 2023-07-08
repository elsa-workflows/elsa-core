using System.Threading.Channels;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.PublishingStrategies;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.HostedServices;

/// <summary>
/// Continuously reads from a channel to which notifications can be sent, publishing each received notification.
/// </summary>
public class BackgroundEventPublisherHostedService : BackgroundService
{
    private readonly int _workerCount;
    private readonly ChannelReader<INotification> _channelReader;
    private readonly IEventPublisher _eventPublisher;
    private readonly IList<Channel<INotification>> _outputs;
    private readonly ILogger _logger;

    /// <inheritdoc />
    public BackgroundEventPublisherHostedService(int workerCount, ChannelReader<INotification> channelReader, IEventPublisher eventPublisher, ILogger<BackgroundEventPublisherHostedService> logger)
    {
        _workerCount = workerCount;
        _channelReader = channelReader;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _outputs = new List<Channel<INotification>>(workerCount);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var index = 0;

        for (var i = 0; i < _workerCount; i++)
        {
            var output = Channel.CreateUnbounded<INotification>();
            _outputs.Add(output);
            _ = ReadOutputAsync(output, cancellationToken);
        }

        await foreach (var notification in _channelReader.ReadAllAsync(cancellationToken))
        {
            var output = _outputs[index];
            await output.Writer.WriteAsync(notification, cancellationToken);
            index = (index + 1) % _workerCount;
        }

        foreach (var output in _outputs)
        {
            output.Writer.Complete();
        }
    }

    private async Task ReadOutputAsync(Channel<INotification> output, CancellationToken cancellationToken)
    {
        await foreach (var notification in output.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await _eventPublisher.PublishAsync(notification, new SequentialProcessingStrategy(), cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unhandled exception occured while processing the queue");
            }
        }
    }
}