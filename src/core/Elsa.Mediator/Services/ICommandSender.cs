namespace Elsa.Mediator.Services;

public interface ICommandSender
{
    Task<T> ExecuteAsync<T>(ICommand<T> command, CancellationToken cancellationToken = default);
    Task ExecuteAsync(ICommand command, CancellationToken cancellationToken = default);
}