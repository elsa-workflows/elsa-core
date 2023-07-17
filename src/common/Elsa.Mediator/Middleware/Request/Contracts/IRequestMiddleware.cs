namespace Elsa.Mediator.Middleware.Request.Contracts;

/// <summary>
/// Represents a request middleware component.
/// </summary>
public interface IRequestMiddleware
{
    /// <summary>
    /// Invokes the request middleware.
    /// </summary>
    /// <param name="context">The request context.</param>
    ValueTask InvokeAsync(RequestContext context);
}