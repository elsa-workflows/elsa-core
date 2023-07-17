using System.Threading.Channels;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.HostedServices;

/// <summary>
/// Continuously reads from a channel to which notifications can be sent, publishing each received notification.
/// </summary>
public class BackgroundEventPublisherHostedService : BackgroundService
{
    private readonly int _workerCount;
    private readonly INotificationsChannel _notificationsChannel;
    private readonly INotificationSender _notificationSender;
    private readonly IList<Channel<INotification>> _outputs;
    private readonly ILogger _logger;

    /// <inheritdoc />
    public BackgroundEventPublisherHostedService(int workerCount, INotificationsChannel notificationsChannel, INotificationSender notificationSender, ILogger<BackgroundEventPublisherHostedService> logger)
    {
        _workerCount = workerCount;
        _notificationsChannel = notificationsChannel;
        _notificationSender = notificationSender;
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

        var channelReader = _notificationsChannel.Reader;
        
        await foreach (var notification in channelReader.ReadAllAsync(cancellationToken))
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

    private async Task ReadOutputAsync(Channel<INotification, INotification> output, CancellationToken cancellationToken)
    {
        await foreach (var notification in output.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await _notificationSender.SendAsync(notification, NotificationStrategy.Parallel, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unhandled exception occured while processing the queue");
            }
        }
    }
}