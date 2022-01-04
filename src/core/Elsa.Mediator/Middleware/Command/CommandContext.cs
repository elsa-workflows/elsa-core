using Elsa.Mediator.Contracts;

namespace Elsa.Mediator.Middleware.Command;

public class CommandContext
{
    public CommandContext(ICommand command, Type resultType, CancellationToken cancellationToken)
    {
        Command = command;
        ResultType = resultType;
        CancellationToken = cancellationToken;
    }

    public ICommand Command { get; init; }
    public Type ResultType { get; init; }
    public CancellationToken CancellationToken { get; init; }
    public object? Result { get; set; }
}