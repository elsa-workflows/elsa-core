using Elsa.Mediator.Middleware.Command.Contracts;

namespace Elsa.Mediator.Middleware.Command.Components;

public class CommandLoggingMiddleware : ICommandMiddleware
{
    private readonly CommandMiddlewareDelegate _next;
    public CommandLoggingMiddleware(CommandMiddlewareDelegate next) => _next = next;

    public async ValueTask InvokeAsync(CommandContext context)
    {
        // Invoke next middleware.
        await _next(context);
    }
}