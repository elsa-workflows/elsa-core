namespace Elsa.Mediator.Contracts;

public interface ICommandHandler
{
}

public interface ICommandHandler<in TCommand, TResult> : ICommandHandler where TCommand : ICommand
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand> : ICommandHandler<TCommand, Unit> where TCommand : ICommand
{
}