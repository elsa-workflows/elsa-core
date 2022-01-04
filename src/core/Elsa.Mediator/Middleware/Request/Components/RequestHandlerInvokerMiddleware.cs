using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Request.Contracts;

namespace Elsa.Mediator.Middleware.Request.Components;

public class RequestHandlerInvokerMiddleware : IRequestMiddleware
{
    private readonly RequestMiddlewareDelegate _next;
    private readonly IEnumerable<IRequestHandler> _requestHandlers;

    public RequestHandlerInvokerMiddleware(RequestMiddlewareDelegate next, IEnumerable<IRequestHandler> requestHandlers)
    {
        _next = next;
        _requestHandlers = requestHandlers;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        // Find all handlers for the specified request.
        var request = context.Request;
        var requestType = request.GetType();
        var responseType = context.ResponseType;
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
        var handlers = _requestHandlers.Where(x => handlerType.IsInstanceOfType(x)).ToArray();

        if (!handlers.Any())
            throw new InvalidOperationException($"There is no handler to handle the {requestType.FullName} request");

        if (handlers.Length > 1)
            throw new InvalidOperationException($"Multiple handlers were found to handle the {requestType.FullName} request");

        var handler = handlers.First();
        var handleMethod = handlerType.GetMethod("HandleAsync")!;
        var cancellationToken = context.CancellationToken;
        var task = (Task)handleMethod.Invoke(handler, new object?[] { request, cancellationToken })!;
        await task;
        
        // Get result of task.
        var taskWithReturnType = typeof(Task<>).MakeGenericType(responseType);
        var resultProperty = taskWithReturnType.GetProperty(nameof(Task<object>.Result))!;
        context.Response = resultProperty.GetValue(task);
        
        // Invoke next middleware.
        await _next(context);
    }
}