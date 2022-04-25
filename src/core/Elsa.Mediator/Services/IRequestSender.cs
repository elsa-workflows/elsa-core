namespace Elsa.Mediator.Services;

public interface IRequestSender
{
    Task<T> RequestAsync<T>(IRequest<T> request, CancellationToken cancellationToken = default);
}