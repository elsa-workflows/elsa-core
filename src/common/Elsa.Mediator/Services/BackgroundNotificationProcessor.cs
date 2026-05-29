using System.Threading.Channels;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Notification;
using Elsa.Mediator.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Mediator.Services;

/// <summary>
/// Continuously reads from a channel to which notifications can be sent, publishing each received notification.
/// </summary>
public class BackgroundNotificationProcessor
{
    private readonly int _workerCount;
    private readonly INotificationsChannel _notificationsChannel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundNotificationProcessor"/> class.
    /// </summary>
    public BackgroundNotificationProcessor(IOptions<MediatorOptions> options, INotificationsChannel notificationsChannel, IServiceScopeFactory scopeFactory, ILogger<BackgroundNotificationProcessor> logger)
    {
        _workerCount = options.Value.NotificationWorkerCount;
        _notificationsChannel = notificationsChannel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Runs the processor until cancellation is requested.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var index = 0;
        var outputs = new List<Channel<NotificationContext>>(_workerCount);
        var workers = new List<Task>(_workerCount);

        using var scope = _scopeFactory.CreateScope();
        var notificationSender = scope.ServiceProvider.GetRequiredService<INotificationSender>();

        for (var i = 0; i < _workerCount; i++)
        {
            var output = Channel.CreateUnbounded<NotificationContext>();
            outputs.Add(output);
            workers.Add(ReadOutputAsync(output, notificationSender, cancellationToken));
        }

        try
        {
            await foreach (var notification in _notificationsChannel.Reader.ReadAllAsync(cancellationToken))
            {
                var output = outputs[index];
                await output.Writer.WriteAsync(notification, cancellationToken);
                index = (index + 1) % _workerCount;
            }
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "An operation was cancelled while processing the notification queue");
        }

        foreach (var output in outputs)
            output.Writer.Complete();

        await Task.WhenAll(workers);
    }

    private async Task ReadOutputAsync(Channel<NotificationContext> output, INotificationSender notificationSender, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var notificationContext in output.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    var notification = notificationContext.Notification;
                    using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, notificationContext.CancellationToken);
                    await notificationSender.SendAsync(notification, NotificationStrategy.Sequential, linkedTokenSource.Token);
                }
                catch (OperationCanceledException e)
                {
                    _logger.LogDebug(e, "An operation was cancelled while processing the notification queue");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An unhandled exception occurred while processing the notification queue");
                }
            }
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "An operation was cancelled while processing the notification queue");
        }
    }
}
