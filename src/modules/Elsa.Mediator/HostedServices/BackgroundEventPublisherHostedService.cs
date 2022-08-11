using System.Threading.Channels;
using Elsa.Mediator.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.HostedServices;

/// <summary>
/// Continuously reads from a channel to which notifications can be sent, publishing each received notification.
/// </summary>
public class BackgroundEventPublisherHostedService : BackgroundService
{
    private readonly ChannelReader<INotification> _channelReader;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger _logger;

    public BackgroundEventPublisherHostedService(ChannelReader<INotification> channelReader, IEventPublisher eventPublisher, ILogger<BackgroundEventPublisherHostedService> logger)
    {
        _channelReader = channelReader;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await foreach (var notification in _channelReader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await _eventPublisher.PublishAsync(notification, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unhandled exception occured while processing the queue");
            }
        }
    }
}