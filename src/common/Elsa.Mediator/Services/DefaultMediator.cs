using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Command;
using Elsa.Mediator.Middleware.Command.Contracts;
using Elsa.Mediator.Middleware.Notification;
using Elsa.Mediator.Middleware.Notification.Contracts;
using Elsa.Mediator.Middleware.Request;
using Elsa.Mediator.Middleware.Request.Contracts;
using Elsa.Mediator.Models;
using Elsa.Mediator.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Mediator.Services;

/// <inheritdoc />
public class DefaultMediator : IMediator
{
    private readonly IRequestPipeline _requestPipeline;
    private readonly ICommandPipeline _commandPipeline;
    private readonly INotificationPipeline _notificationPipeline;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventPublishingStrategy _defaultPublishingStrategy;
    private readonly ICommandStrategy _defaultCommandStrategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMediator"/> class.
    /// </summary>
    /// <param name="requestPipeline">The request pipeline.</param>
    /// <param name="commandPipeline">The command pipeline.</param>
    /// <param name="notificationPipeline">The notification pipeline.</param>
    /// <param name="options">The mediator options.</param>
    public DefaultMediator(
        IRequestPipeline requestPipeline,
        ICommandPipeline commandPipeline,
        INotificationPipeline notificationPipeline,
        IOptions<MediatorOptions> options,
        IServiceProvider serviceProvider)
    {
        _requestPipeline = requestPipeline;
        _commandPipeline = commandPipeline;
        _notificationPipeline = notificationPipeline;
        _serviceProvider = serviceProvider;
        _defaultPublishingStrategy = options.Value.DefaultPublishingStrategy;
        _defaultCommandStrategy = options.Value.DefaultCommandStrategy;
    }

    /// <inheritdoc />
    public async Task<T> SendAsync<T>(IRequest<T> request, CancellationToken cancellationToken = default)
    {
        var responseType = typeof(T);
        var context = new RequestContext(request, responseType, _serviceProvider, cancellationToken);
        await _requestPipeline.ExecuteAsync(context);

        return (T)context.Response;
    }

    public async Task<T> SendAsync<T>(ICommand<T> command, ICommandStrategy strategy, IDictionary<object, object> headers, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(T);
        var context = new CommandContext(command, strategy, resultType, headers, _serviceProvider, cancellationToken);
        await _commandPipeline.InvokeAsync(context);

        return (T)context.Result!;
    }

    /// <inheritdoc />
    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default) => await SendAsync(command, _defaultCommandStrategy, cancellationToken);

    /// <inheritdoc />
    public Task SendAsync(ICommand command, ICommandStrategy? strategy = null, CancellationToken cancellationToken = default)
    {
        return SendAsync(command, strategy, new Dictionary<object, object>(), cancellationToken);
    }

    public async Task SendAsync(ICommand command, ICommandStrategy? strategy, IDictionary<object, object> headers, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(Unit);
        strategy ??= _defaultCommandStrategy;
        var context = new CommandContext(command, strategy, resultType, headers, _serviceProvider, cancellationToken);
        await _commandPipeline.InvokeAsync(context);
    }

    /// <inheritdoc />
    public Task<T> SendAsync<T>(ICommand<T> command, CancellationToken cancellationToken = default)
    {
        return SendAsync(command, new Dictionary<object, object>(), cancellationToken);
    }

    public Task<T> SendAsync<T>(ICommand<T> command, IDictionary<object, object> headers, CancellationToken cancellationToken = default)
    {
        return SendAsync(command, _defaultCommandStrategy, headers, cancellationToken);
    }

    /// <inheritdoc />
    public Task<T> SendAsync<T>(ICommand<T> command, ICommandStrategy strategy, CancellationToken cancellationToken = default)
    {
        return SendAsync(command, strategy, new Dictionary<object, object>(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        await SendAsync(notification, _defaultPublishingStrategy, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendAsync(INotification notification, IEventPublishingStrategy? strategy = null, CancellationToken cancellationToken = default)
    {
        strategy ??= _defaultPublishingStrategy;
        var context = new NotificationContext(notification, strategy, _serviceProvider, cancellationToken);
        await _notificationPipeline.ExecuteAsync(context);
    }
}