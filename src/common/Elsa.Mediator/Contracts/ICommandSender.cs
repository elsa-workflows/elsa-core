namespace Elsa.Mediator.Contracts;

/// <summary>
/// Sends a command.
/// </summary>
public interface ICommandSender
{
    /// <summary>
    /// Sends a command using he default strategy.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <returns>The result.</returns>
    Task<T> SendAsync<T>(ICommand<T> command, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a command using the specified strategy.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <param name="strategy">The command strategy to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <returns>The result.</returns>
    Task<T> SendAsync<T>(ICommand<T> command, ICommandStrategy strategy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a command using the default strategy.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a command using the specified strategy.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <param name="strategy">The command strategy to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SendAsync(ICommand command, ICommandStrategy strategy, CancellationToken cancellationToken = default);
}