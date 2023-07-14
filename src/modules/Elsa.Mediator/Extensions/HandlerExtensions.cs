using System.Reflection;
using Elsa.Mediator.Contracts;

namespace Elsa.Mediator.Extensions;

/// <summary>
/// Contains helper methods for invoking notification handlers.
/// </summary>
public static class HandlerExtensions
{
    /// <summary>
    /// Gets the handle method for a notification handler of the given notification type.
    /// </summary>
    /// <param name="notificationType">The notification type.</param>
    /// <returns>The handle method.</returns>
    public static MethodInfo GetNotificationHandlerMethod(this Type notificationType)
    {
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
        return handlerType.GetMethod("HandleAsync")!;
    }

    /// <summary>
    /// Gets the handle method for a command handler of the given command type.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <returns>The handle method.</returns>
    public static MethodInfo GetCommandHandlerMethod(this Type commandType)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);
        return handlerType.GetMethod("HandleAsync")!;
    }
    
    /// <summary>
    /// Invokes the given handler for the given notification.
    /// </summary>
    /// <param name="handler">The handler to invoke.</param>
    /// <param name="handleMethod">The handle method.</param>
    /// <param name="notification">The notification to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static Task InvokeAsync(this INotificationHandler handler, MethodBase handleMethod, INotification notification, CancellationToken cancellationToken)
    {
        return (Task)handleMethod.Invoke(handler, new object?[] { notification, cancellationToken })!;
    }

    /// <summary>
    /// Invokes the given handler for the given command.
    /// </summary>
    /// <param name="handler">The handler to invoke.</param>
    /// <param name="handleMethod">The handle method.</param>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static Task InvokeAsync(this ICommandHandler handler, MethodBase handleMethod, ICommand command, CancellationToken cancellationToken)
    {
        return (Task)handleMethod.Invoke(handler, new object?[] { command, cancellationToken })!;
    }
}