namespace Elsa.Mediator.Contracts;

/// <summary>
/// Represents a request handler.
/// </summary>
public interface IRequestHandler
{
}

/// <summary>
/// Represents a request handler.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IRequestHandler<in TRequest, TResponse> : IRequestHandler where TRequest : IRequest<TResponse>?
{
    /// <summary>
    /// Handles the given request.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}