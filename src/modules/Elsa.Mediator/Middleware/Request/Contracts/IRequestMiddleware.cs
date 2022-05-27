namespace Elsa.Mediator.Middleware.Request.Contracts;

public interface IRequestMiddleware
{
    ValueTask InvokeAsync(RequestContext context);
}