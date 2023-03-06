using Elsa.Mediator.Contracts;

namespace Elsa.Mediator.Middleware.Request;

public class RequestContext
{
    public RequestContext(IRequest request, Type responseType, CancellationToken cancellationToken)
    {
        Request = request;
        ResponseType = responseType;
        CancellationToken = cancellationToken;
    }

    public IRequest Request { get; init; }
    public Type ResponseType { get; init; }
    public CancellationToken CancellationToken { get; init; }
    public object? Response { get; set; }
}