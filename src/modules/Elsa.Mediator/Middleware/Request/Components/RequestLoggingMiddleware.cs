using Elsa.Mediator.Middleware.Request.Contracts;

namespace Elsa.Mediator.Middleware.Request.Components;

public class RequestLoggingMiddleware : IRequestMiddleware
{
    private readonly RequestMiddlewareDelegate _next;
    public RequestLoggingMiddleware(RequestMiddlewareDelegate next) => _next = next;

    public async ValueTask InvokeAsync(RequestContext context)
    {
        // Invoke next middleware.
        await _next(context);
    }
}