using System.Threading.Channels;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Notification;
using Elsa.Mediator.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Mediator.HostedServices;

/// <summary>
/// Continuously reads from a channel to which notifications can be sent, publishing each received notification.
/// </summary>
public class BackgroundEventPublisherHostedService : BackgroundService
{
    private readonly int _workerCount;
    private readonly INotificationsChannel _notificationsChannel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly List<Channel<NotificationContext>> _outputs;
    private readonly ILogger _logger;

    /// <inheritdoc />
    public BackgroundEventPublisherHostedService(IOptions<MediatorOptions> options, INotificationsChannel notificationsChannel, IServiceScopeFactory scopeFactory, ILogger<BackgroundEventPublisherHostedService> logger)
    {
        _workerCount = options.Value.NotificationWorkerCount;
        _notificationsChannel = notificationsChannel;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _outputs = new(_workerCount);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var index = 0;

        using var scope = _scopeFactory.CreateScope();
        var notificationSender = scope.ServiceProvider.GetRequiredService<INotificationSender>();

        for (var i = 0; i < _workerCount; i++)
        {
            var output = Channel.CreateUnbounded<NotificationContext>();
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

    private async Task ReadOutputAsync(Channel<NotificationContext> output, INotificationSender notificationSender, CancellationToken cancellationToken)
    {
        await foreach (var notificationContext in output.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                var notification = notificationContext.Notification;
                await notificationSender.SendAsync(notification, NotificationStrategy.Sequential, notificationContext.CancellationToken);
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