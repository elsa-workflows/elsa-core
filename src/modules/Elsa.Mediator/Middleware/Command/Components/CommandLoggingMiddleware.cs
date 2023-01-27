using Elsa.Mediator.Middleware.Command.Contracts;
using Elsa.Mediator.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.Middleware.Command.Components;

/// <summary>
/// A command middleware that logs the command being invoked.
/// </summary>
public class CommandLoggingMiddleware : ICommandMiddleware
{
    private readonly CommandMiddlewareDelegate _next;
    private readonly ILogger<CommandLoggingMiddleware> _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public CommandLoggingMiddleware(CommandMiddlewareDelegate next, ILogger<CommandLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask InvokeAsync(CommandContext context)
    {
        var commandType = context.Command.GetType();
        _logger.LogInformation("Invoking {CommandName}", commandType.Name);
        
        await _next(context);
        
        if (context.Result is null or Unit)
            _logger.LogInformation("{CommandName} completed with no result", commandType.Name);
        else
            _logger.LogInformation("{CommandName} completed wit result {CommandResult}", commandType.Name, context.Result);
    }
}