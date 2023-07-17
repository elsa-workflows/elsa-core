using Elsa.Mediator.Middleware.Notification.Contracts;

namespace Elsa.Mediator.Middleware.Notification;

/// <inheritdoc />
public class NotificationPipelineBuilder : INotificationPipelineBuilder
{
    private const string ServicesKey = "mediator.Services";
    private readonly IList<Func<NotificationMiddlewareDelegate, NotificationMiddlewareDelegate>> _components = new List<Func<NotificationMiddlewareDelegate, NotificationMiddlewareDelegate>>();

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationPipelineBuilder"/> class.
    /// </summary>
    public NotificationPipelineBuilder(IServiceProvider serviceProvider)
    {
        ApplicationServices = serviceProvider;
    }

    /// <inheritdoc />
    public IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>();

    /// <inheritdoc />
    public IServiceProvider ApplicationServices
    {
        get => GetProperty<IServiceProvider>(ServicesKey)!;
        set => SetProperty(ServicesKey, value);
    }

    /// <inheritdoc />
    public INotificationPipelineBuilder Use(Func<NotificationMiddlewareDelegate, NotificationMiddlewareDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }

    /// <inheritdoc />
    public NotificationMiddlewareDelegate Build()
    {
        NotificationMiddlewareDelegate pipeline = _ => new ValueTask();

        foreach (var component in _components.Reverse())
            pipeline = component(pipeline);

        return pipeline;
    }

    private T? GetProperty<T>(string key) => Properties.TryGetValue(key, out var value) ? (T?)value : default(T);
    private void SetProperty<T>(string key, T value) => Properties[key] = value;
}