namespace Elsa.Mediator.Services;

public interface IRequestHandler
{
}

public interface IRequestHandler<in TRequest, TResponse> : IRequestHandler where TRequest : IRequest<TResponse>?
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}