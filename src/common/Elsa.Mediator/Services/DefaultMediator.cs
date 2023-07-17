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
        IOptions<MediatorOptions> options)
    {
        _requestPipeline = requestPipeline;
        _commandPipeline = commandPipeline;
        _notificationPipeline = notificationPipeline;
        _defaultPublishingStrategy = options.Value.DefaultPublishingStrategy;
        _defaultCommandStrategy = options.Value.DefaultCommandStrategy;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> SendAsync<T>(IRequest<T> request, CancellationToken cancellationToken = default)
    {
        var responseType = typeof(T);
        var context = new RequestContext(request, responseType, cancellationToken);
        await _requestPipeline.ExecuteAsync(context);

        if (context.Responses.All(x => x is T))
            return context.Responses.Cast<T>().AsEnumerable();

        throw new InvalidCastException($"Unable to cast objects in Responses property to type {typeof(T)}");
    }

    /// <inheritdoc />
    public async Task SendAsync(ICommand command, ICommandStrategy? strategy = default, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(Unit);
        strategy ??= _defaultCommandStrategy;
        var context = new CommandContext(command, strategy, resultType, cancellationToken);
        await _commandPipeline.InvokeAsync(context);
    }

    /// <inheritdoc />
    public async Task<T> SendAsync<T>(ICommand<T> command, ICommandStrategy? strategy = default, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(T);
        strategy ??= _defaultCommandStrategy;
        var context = new CommandContext(command, strategy, resultType, cancellationToken);
        await _commandPipeline.InvokeAsync(context);

        return (T)context.Result!;
    }

    /// <inheritdoc />
    public async Task SendAsync(INotification notification, IEventPublishingStrategy? strategy = default, CancellationToken cancellationToken = default)
    {
        strategy ??= _defaultPublishingStrategy;
        var context = new NotificationContext(notification, strategy, cancellationToken);
        await _notificationPipeline.ExecuteAsync(context);
    }
}