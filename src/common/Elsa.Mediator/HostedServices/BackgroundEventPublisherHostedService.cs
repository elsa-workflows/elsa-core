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
        // Index to round-robin distribute notifications across worker channels
        var index = 0;

        using var scope = _scopeFactory.CreateScope();
        var notificationSender = scope.ServiceProvider.GetRequiredService<INotificationSender>();

        // Create multiple output channels and start worker tasks for parallel processing
        for (var i = 0; i < _workerCount; i++)
        {
            var output = Channel.CreateUnbounded<NotificationContext>();
            _outputs.Add(output);
            // Start a background task to process notifications from this output channel
            _ = ReadOutputAsync(output, notificationSender, cancellationToken);
        }

        var channelReader = _notificationsChannel.Reader;

        // Continuously read notifications from the input channel and distribute them to worker channels
        // using round-robin distribution for load balancing
        await foreach (var notification in channelReader.ReadAllAsync(cancellationToken))
        {
            var output = _outputs[index];
            await output.Writer.WriteAsync(notification, cancellationToken);
            // Move to the next worker in a circular fashion
            index = (index + 1) % _workerCount;
        }

        // When the input channel is completed, complete all output channels
        foreach (var output in _outputs)
        {
            output.Writer.Complete();
        }
    }

    /// <summary>
    /// Processes notifications from an output channel asynchronously.
    /// </summary>
    /// <param name="output">The channel to read notifications from</param>
    /// <param name="notificationSender">The service used to send notifications</param>
    /// <param name="cancellationToken">Cancellation token from the hosted service</param>
    private async Task ReadOutputAsync(Channel<NotificationContext> output, INotificationSender notificationSender, CancellationToken cancellationToken)
    {
        await foreach (var notificationContext in output.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                var notification = notificationContext.Notification;
                // Link the cancellation tokens so that cancellation can happen from either source
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, notificationContext.CancellationToken);
                await notificationSender.SendAsync(notification, NotificationStrategy.Sequential, linkedTokenSource.Token);
            }
            catch (OperationCanceledException e)
            {
                _logger.LogDebug(e, "An operation was cancelled while processing the queue");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unhandled exception occurred while processing the queue");
            }
        }
    }
}