namespace Elsa.Mediator.Contracts;

public interface IRequestSender
{
    Task<T> RequestAsync<T>(IRequest<T> request, CancellationToken cancellationToken = default);
}