using Elsa.Mediator.Contracts;

namespace Elsa.Mediator.Middleware.Command;

/// <summary>
/// Provides context for a command.
/// </summary>
public class CommandContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandContext"/> class.
    /// </summary>
    public CommandContext(ICommand command, Type resultType, CancellationToken cancellationToken)
    {
        Command = command;
        ResultType = resultType;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the command.
    /// </summary>
    public ICommand Command { get; init; }
    
    /// <summary>
    /// Gets the result type.
    /// </summary>
    public Type ResultType { get; init; }
    
    /// <summary>
    /// Gets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }
    
    /// <summary>
    /// Gets or sets the result.
    /// </summary>
    public object? Result { get; set; }
}