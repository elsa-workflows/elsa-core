using System.Diagnostics.CodeAnalysis;
using Elsa.Mediator.Contexts;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Command.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Mediator.Middleware.Command.Components;

/// <summary>
/// A command middleware that invokes the command.
/// </summary>
[UsedImplicitly]
public class CommandHandlerInvokerMiddleware(CommandMiddlewareDelegate next) : ICommandMiddleware
{
    /// <inheritdoc />
    [UnconditionalSuppressMessage("Trimming", "IL2060:Call to MakeGenericMethod can not be statically analyzed", Justification = "The result type is determined at runtime from command types and handlers are registered in DI.")]
    public async ValueTask InvokeAsync(CommandContext context)
    {
        // Find all handlers for the specified command.
        var command = context.Command;
        var commandType = command.GetType();
        var resultType = context.ResultType;
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, resultType);
        var serviceProvider = context.ServiceProvider;
        var commandHandlers = serviceProvider.GetServices<ICommandHandler>();
        var handlers = commandHandlers.DistinctBy(x => x.GetType()).Where(x => handlerType.IsInstanceOfType(x)).ToArray();

        if (handlers.Length == 0)
            throw new InvalidOperationException($"There is no handler to handle the {commandType.FullName} command");

        if (handlers.Length > 1)
            throw new InvalidOperationException($"Multiple handlers were found to handle the {commandType.FullName} command");

        var handler = handlers.First();
        var strategyContext = new CommandStrategyContext(context, handler, serviceProvider, context.CancellationToken);
        var strategy = context.CommandStrategy;
        var executeMethod = strategy.GetType().GetMethod(nameof(ICommandStrategy.ExecuteAsync))!;
        var executeMethodWithReturnType = executeMethod.MakeGenericMethod(resultType);

        // Execute command.
        var task = (Task)executeMethodWithReturnType.Invoke(strategy, [strategyContext])!;

        // Wait for completion.
        await task;

        // Get the result of the task.
        var taskWithReturnType = typeof(Task<>).MakeGenericType(resultType);
        var resultProperty = taskWithReturnType.GetProperty(nameof(Task<object>.Result))!;
        context.Result = resultProperty.GetValue(task);

        // Invoke next middleware.
        await next(context).ConfigureAwait(false);
    }
}