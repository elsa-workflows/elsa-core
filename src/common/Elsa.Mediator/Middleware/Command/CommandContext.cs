using Elsa.Mediator.Contracts;

namespace Elsa.Mediator.Middleware.Command;

/// <summary>
/// Provides context for a command.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CommandContext"/> class.
/// </remarks>
public class CommandContext(ICommand command,
    ICommandStrategy commandStrategy,
    Type resultType, CancellationToken cancellationToken)
{

    /// <summary>
    /// Gets the command.
    /// </summary>
    public ICommand Command { get; init; } = command;

    /// <summary>
    /// Gets the command strategy.
    /// </summary>
    public ICommandStrategy CommandStrategy { get; } = commandStrategy;

    /// <summary>
    /// Gets the result type.
    /// </summary>
    public Type ResultType { get; init; } = resultType;

    /// <summary>
    /// Gets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = cancellationToken;

    /// <summary>
    /// Gets or sets the result.
    /// </summary>
    public object? Result { get; set; }
}