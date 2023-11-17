using Elsa.Mediator.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Elsa.Mediator.HostedServices;

/// <summary>
/// Continuously reads from a channel to which notifications can be sent, publishing each received notification.
/// </summary>
public class BackgroundEventPublisherHostedService : BackgroundService
{
    private readonly int _workerCount;
    private readonly INotificationsChannel _notificationsChannel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IList<Channel<INotification>> _outputs;
    private readonly ILogger _logger;

    /// <inheritdoc />
    public BackgroundEventPublisherHostedService(int workerCount, INotificationsChannel notificationsChannel, IServiceScopeFactory scopeFactory, ILogger<BackgroundEventPublisherHostedService> logger)
    {
        _workerCount = workerCount;
        _notificationsChannel = notificationsChannel;
        _scopeFactory = scopeFactory;
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

    private async Task ReadOutputAsync(Channel<INotification> output, CancellationToken cancellationToken)
    {
        await foreach (var notification in output.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var notificationSender = scope.ServiceProvider.GetRequiredService<INotificationSender>();

                await notificationSender.SendAsync(notification, NotificationStrategy.FireAndForget, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unhandled exception occured while processing the queue");
            }
        }
    }
}
