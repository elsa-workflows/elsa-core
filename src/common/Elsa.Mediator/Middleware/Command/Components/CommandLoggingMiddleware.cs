using Elsa.Mediator.Middleware.Command.Contracts;
using Elsa.Mediator.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.Middleware.Command.Components;

/// <summary>
/// A command middleware that logs the command being invoked.
/// </summary>
[UsedImplicitly]
public class CommandLoggingMiddleware(CommandMiddlewareDelegate next, ILogger<CommandLoggingMiddleware> logger) : ICommandMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(CommandContext context)
    {
        var commandType = context.Command.GetType();
        logger.LogInformation("Invoking {CommandName}", commandType.Name);
        
        await next(context);
        
        if (context.Result is null or Unit)
            logger.LogInformation("{CommandName} completed with no result", commandType.Name);
        else
            logger.LogInformation("{CommandName} completed wit result {CommandResult}", commandType.Name, context.Result);
    }
}