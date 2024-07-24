using System.Threading.Channels;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.HostedServices;

/// <summary>
/// Continuously reads from a channel to which notifications can be sent, publishing each received notification.
/// </summary>
/// <inheritdoc />
public class BackgroundEventPublisherHostedService(int workerCount,
    INotificationsChannel notificationsChannel, 
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundEventPublisherHostedService> logger) : BackgroundService
{
    private readonly int _workerCount = workerCount;
    private readonly INotificationsChannel _notificationsChannel = notificationsChannel;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly List<Channel<INotification>> _outputs = new List<Channel<INotification>>(workerCount);
    private readonly ILogger _logger = logger;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var index = 0;

        using var scope = _scopeFactory.CreateScope();
        var notificationSender = scope.ServiceProvider.GetRequiredService<INotificationSender>();

        for (var i = 0; i < _workerCount; i++)
        {
            var output = Channel.CreateUnbounded<INotification>();
            _outputs.Add(output);
            _ = ReadOutputAsync(output, notificationSender, cancellationToken);
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

    private async Task ReadOutputAsync(Channel<INotification> output, INotificationSender notificationSender, CancellationToken cancellationToken)
    {
        await foreach (var notification in output.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await notificationSender.SendAsync(notification, NotificationStrategy.Sequential, cancellationToken);
            }
            catch (OperationCanceledException e)
            {
                _logger.LogDebug(e, "An operation was cancelled while processing the queue");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unhandled exception occured while processing the queue");
            }
        }
    }
}