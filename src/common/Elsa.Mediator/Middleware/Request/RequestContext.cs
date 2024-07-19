using Elsa.Mediator.Contracts;

namespace Elsa.Mediator.Middleware.Request;

/// <summary>
/// Provides context to a request handler.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RequestContext"/> class.
/// </remarks>
/// <param name="request">The request.</param>
/// <param name="responseType">The response type.</param>
/// <param name="cancellationToken">The cancellation token.</param>
public class RequestContext(IRequest request, 
    Type responseType,
    CancellationToken cancellationToken)
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
    /// Gets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = cancellationToken;

    /// <summary>
    /// Gets the responses from each request handler.
    /// </summary>
    public ICollection<object> Responses { get; set; } = new List<object>();
}