using Elsa.Mediator.Contexts;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Command;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Mediator.CommandStrategies;

/// <summary>
/// Invokes command handlers in the background and does not wait for the result.
/// </summary>
public class BackgroundStrategy : ICommandStrategy
{
    /// <inheritdoc />
    public async Task<TResult> ExecuteAsync<TResult>(CommandStrategyContext context)
    {
        var commandsChannel = context.ServiceProvider.GetRequiredService<ICommandsChannel>();
        var commandContext = context.CommandContext;
        var queuedContext = new CommandContext(
            commandContext.Command,
            CommandStrategy.Default,
            commandContext.ResultType,
            commandContext.Headers,
            commandContext.ServiceProvider,
            CancellationToken.None);
        await commandsChannel.Writer.WriteAsync(queuedContext, context.CancellationToken);
        return default!;
    }
}
