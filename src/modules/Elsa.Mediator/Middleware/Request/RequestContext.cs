using Elsa.Mediator.Contracts;

namespace Elsa.Mediator.Middleware.Request;

/// <summary>
/// Provides context to a request handler.
/// </summary>
public class RequestContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestContext"/> class.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="responseType">The response type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public RequestContext(IRequest request, Type responseType, CancellationToken cancellationToken)
    {
        Request = request;
        ResponseType = responseType;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the request.
    /// </summary>
    public IRequest Request { get; init; }
    
    /// <summary>
    /// Gets the response type.
    /// </summary>
    public Type ResponseType { get; init; }
    
    /// <summary>
    /// Gets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }
    
    /// <summary>
    /// Gets the responses from each request handler.
    /// </summary>
    public ICollection<object> Responses { get; set; } = new List<object>();
}