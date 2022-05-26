namespace Elsa.Mediator.Middleware.Command.Contracts;

public interface ICommandMiddleware
{
    ValueTask InvokeAsync(CommandContext context);
}