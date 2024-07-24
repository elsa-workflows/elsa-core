using Elsa.Mediator.Middleware.Request.Contracts;

namespace Elsa.Mediator.Middleware.Request.Components;

/// <summary>
/// A middleware that logs the request.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RequestLoggingMiddleware"/> class.
/// </remarks>
/// <param name="next">The next middleware in the pipeline.</param>
public class RequestLoggingMiddleware(RequestMiddlewareDelegate next) : IRequestMiddleware
{
    private readonly RequestMiddlewareDelegate _next = next;

    /// <inheritdoc />
    public async ValueTask InvokeAsync(RequestContext context)
    {
        // Invoke next middleware.
        await _next(context);
    }
}