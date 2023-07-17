using Elsa.Mediator.Middleware.Notification.Contracts;

namespace Elsa.Mediator.Middleware.Notification;

/// <inheritdoc />
public class NotificationPipeline : INotificationPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private NotificationMiddlewareDelegate? _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationPipeline"/> class.
    /// </summary>
    public NotificationPipeline(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    /// <inheritdoc />
    public NotificationMiddlewareDelegate Pipeline => _pipeline ??= CreateDefaultPipeline();

    /// <inheritdoc />
    public NotificationMiddlewareDelegate Setup(Action<INotificationPipelineBuilder>? setup = default)
    {
        var builder = new NotificationPipelineBuilder(_serviceProvider);
        setup?.Invoke(builder);
        _pipeline = builder.Build();
        return _pipeline;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(NotificationContext context) => await Pipeline(context);

    private NotificationMiddlewareDelegate CreateDefaultPipeline() => Setup(x => x.UseNotificationHandlers());
}