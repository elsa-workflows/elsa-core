using Elsa.Mediator.Middleware.Notification.Contracts;

namespace Elsa.Mediator.Middleware.Notification;

public class NotificationPipeline : INotificationPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private NotificationMiddlewareDelegate? _pipeline;

    public NotificationPipeline(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
    public NotificationMiddlewareDelegate Pipeline => _pipeline ??= CreateDefaultPipeline();

    public NotificationMiddlewareDelegate Setup(Action<INotificationPipelineBuilder>? setup = default)
    {
        var builder = new NotificationPipelineBuilder(_serviceProvider);
        setup?.Invoke(builder);
        _pipeline = builder.Build();
        return _pipeline;
    }

    public async Task ExecuteAsync(NotificationContext context) => await Pipeline(context);

    private NotificationMiddlewareDelegate CreateDefaultPipeline() => Setup(x => x.UseNotificationHandlers());
}