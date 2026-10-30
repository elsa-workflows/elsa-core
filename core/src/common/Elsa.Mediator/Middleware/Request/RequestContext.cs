using Elsa.Mediator.Contracts;

namespace Elsa.Mediator.Middleware.Request;

/// <summary>
/// Provides context to a request handler.
/// </summary>
public class RequestContext(IRequest request, Type responseType, IServiceProvider serviceProvider, CancellationToken cancellationToken)
{
    /// <summary>
    /// Gets the request.
    /// </summary>
    public IRequest Request { get; init; } = request;

    /// <summary>
    /// Gets the response type.
    /// </summary>
    public Type ResponseType { get; init; } = responseType;

    /// <summary>
    /// Gets the service provider used for resolving dependencies within the request context.
    /// </summary>
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    /// <summary>
    /// Gets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = cancellationToken;

    /// <summary>
    /// Gets the response the request handler.
    /// </summary>
    public object Response { get; set; } = null!;
}