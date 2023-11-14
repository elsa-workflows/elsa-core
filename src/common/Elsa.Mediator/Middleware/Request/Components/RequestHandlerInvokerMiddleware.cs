using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Request.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Mediator.Middleware.Request.Components;

/// <summary>
/// A middleware component that invokes request handlers.
/// </summary>
public class RequestHandlerInvokerMiddleware : IRequestMiddleware
{
    private readonly RequestMiddlewareDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestHandlerInvokerMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="scopeFactory">Scope factory to create the scope and get dependancies.</param>
    public RequestHandlerInvokerMiddleware(RequestMiddlewareDelegate next, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async ValueTask InvokeAsync(RequestContext context)
    {
        using var scope = _scopeFactory.CreateScope();
        var requestHandlers = scope.ServiceProvider.GetServices<IRequestHandler>();

        // Find all handlers for the specified request.
        var request = context.Request;
        var requestType = request.GetType();
        var responseType = context.ResponseType;
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
        var handlers = requestHandlers.Where(x => handlerType.IsInstanceOfType(x)).ToArray();

        foreach (var handler in handlers)
        {
            var handleMethod = handlerType.GetMethod("HandleAsync")!;
            var cancellationToken = context.CancellationToken;
            var task = (Task)handleMethod.Invoke(handler, new object?[] { request, cancellationToken })!;
            await task;

            // Get result of task.
            var taskWithReturnType = typeof(Task<>).MakeGenericType(responseType);
            var resultProperty = taskWithReturnType.GetProperty(nameof(Task<object>.Result))!;
            context.Responses.Add(resultProperty.GetValue(task)!);
        }

        // Invoke next middleware.
        await _next(context);
    }
}