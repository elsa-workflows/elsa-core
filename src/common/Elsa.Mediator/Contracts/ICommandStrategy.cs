using Elsa.Mediator.Contexts;

namespace Elsa.Mediator.Contracts;

/// <summary>
/// Represents a strategy for executing commands.
/// </summary>
public interface ICommandStrategy
{
    /// <summary>
    /// Executes a command.
    /// </summary>
    /// <param name="context">The context for executing the command.</param>
    Task<TResult> ExecuteAsync<TResult>(CommandStrategyContext context);
}