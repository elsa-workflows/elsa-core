using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Command;
using Elsa.Mediator.Middleware.Notification;
using Elsa.Mediator.Models;

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
    public static MethodInfo GetCommandHandlerMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type commandType)
    {
        var commandTypeInterfaces = commandType.GetInterfaces();
        var commandTypeInterface = commandTypeInterfaces.FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICommand<>));
        var resultType = commandTypeInterface?.GetGenericArguments()[0] ?? typeof(Unit);
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, resultType);
        return handlerType.GetMethod("HandleAsync")!;
    }
    
    /// <summary>
    /// Invokes the given handler for the given notification.
    /// </summary>
    /// <param name="handler">The handler to invoke.</param>
    /// <param name="handleMethod">The handle method.</param>
    /// <param name="notificationContext">The notification context containing the notification and cancellation token.</param>
    public static Task InvokeAsync(this INotificationHandler handler, MethodBase handleMethod, NotificationContext notificationContext)
    {
        var notification = notificationContext.Notification;
        var cancellationToken = notificationContext.CancellationToken;
        return InvokeAndUnwrap<Task>(handleMethod, handler, [notification, cancellationToken]);
    }

    /// <summary>
    /// Invokes the given handler for the given command.
    /// </summary>
    /// <param name="handler">The handler to invoke.</param>
    /// <param name="handleMethod">The handle method.</param>
    /// <param name="commandContext">The command to handle.</param>
    public static Task<TResult> InvokeAsync<TResult>(this ICommandHandler handler, MethodBase handleMethod, CommandContext commandContext)
    {
        var command = commandContext.Command;
        var cancellationToken = commandContext.CancellationToken;
        return InvokeAndUnwrap<Task<TResult>>(handleMethod, handler, [command, cancellationToken]);
    }

    /// <summary>
    /// Invokes a method via reflection and unwraps any TargetInvocationException to preserve the original exception's stack trace.
    /// </summary>
    private static T InvokeAndUnwrap<T>(MethodBase method, object target, object[] args) where T : Task
    {
        try
        {
            return (T)method.Invoke(target, args)!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // Unreachable, but required for compiler
        }
    }
}