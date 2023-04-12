using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Command;
using Elsa.Mediator.Middleware.Command.Contracts;
using Elsa.Mediator.Middleware.Notification;
using Elsa.Mediator.Middleware.Notification.Contracts;
using Elsa.Mediator.Middleware.Request;
using Elsa.Mediator.Middleware.Request.Contracts;
using Elsa.Mediator.Models;

namespace Elsa.Mediator.Services;

public class DefaultMediator : IMediator
{
    private readonly IRequestPipeline _requestPipeline;
    private readonly ICommandPipeline _commandPipeline;
    private readonly INotificationPipeline _notificationPipeline;

    public DefaultMediator(
        IRequestPipeline requestPipeline,
        ICommandPipeline commandPipeline,
        INotificationPipeline notificationPipeline)
    {
        _requestPipeline = requestPipeline;
        _commandPipeline = commandPipeline;
        _notificationPipeline = notificationPipeline;
    }

    public async Task<T> RequestAsync<T>(IRequest<T> request, CancellationToken cancellationToken = default)
    {
        var responseType = typeof(T);
        var context = new RequestContext(request, responseType, cancellationToken);
        await _requestPipeline.ExecuteAsync(context);

        return (T)context.Response!;
    }

    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(Unit);
        var context = new CommandContext(command, resultType, cancellationToken);
        await _commandPipeline.InvokeAsync(context);
    }

    public async Task<T> SendAsync<T>(ICommand<T> command, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(T);
        var context = new CommandContext(command, resultType, cancellationToken);
        await _commandPipeline.InvokeAsync(context);

        return (T)context.Result!;
    }

    public async Task PublishAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        var context = new NotificationContext(notification, cancellationToken);
        await _notificationPipeline.ExecuteAsync(context);
    }
}