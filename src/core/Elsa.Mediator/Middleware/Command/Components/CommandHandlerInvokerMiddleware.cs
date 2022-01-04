using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Command.Contracts;

namespace Elsa.Mediator.Middleware.Command.Components;

public class CommandHandlerInvokerMiddleware : ICommandMiddleware
{
    private readonly CommandMiddlewareDelegate _next;
    private readonly IEnumerable<ICommandHandler> _commandHandlers;

    public CommandHandlerInvokerMiddleware(CommandMiddlewareDelegate next, IEnumerable<ICommandHandler> commandHandlers)
    {
        _next = next;
        _commandHandlers = commandHandlers;
    }

    public async ValueTask InvokeAsync(CommandContext context)
    {
        // Find all handlers for the specified command.
        var command = context.Command;
        var commandType = command.GetType();
        var resultType = context.ResultType;
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, resultType);
        var handlers = _commandHandlers.Where(x => handlerType.IsInstanceOfType(x)).ToArray();

        if (!handlers.Any())
            throw new InvalidOperationException($"There is no handler to handle the {commandType.FullName} command");

        if (handlers.Length > 1)
            throw new InvalidOperationException($"Multiple handlers were found to handle the {commandType.FullName} command");

        var handler = handlers.First();
        var handleMethod = handlerType.GetMethod("HandleAsync")!;
        var cancellationToken = context.CancellationToken;
        var task = (Task)handleMethod.Invoke(handler, new object?[] { command, cancellationToken })!;
        await task;
        
        // Get result of task.
        var taskWithReturnType = typeof(Task<>).MakeGenericType(resultType);
        var resultProperty = taskWithReturnType.GetProperty(nameof(Task<object>.Result))!;
        context.Result = resultProperty.GetValue(task);
        
        // Invoke next middleware.
        await _next(context);
    }
}