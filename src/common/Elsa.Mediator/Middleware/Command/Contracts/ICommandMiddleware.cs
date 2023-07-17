namespace Elsa.Mediator.Middleware.Command.Contracts;

/// <summary>
/// Represents a command middleware.
/// </summary>
public interface ICommandMiddleware
{
    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The command context.</param>
    ValueTask InvokeAsync(CommandContext context);
}