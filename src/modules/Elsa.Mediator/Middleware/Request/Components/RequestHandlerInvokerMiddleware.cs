using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Request.Contracts;

namespace Elsa.Mediator.Middleware.Request.Components;

/// <summary>
/// A middleware component that invokes request handlers.
/// </summary>
public class RequestHandlerInvokerMiddleware : IRequestMiddleware
{
    private readonly RequestMiddlewareDelegate _next;
    private readonly IEnumerable<IRequestHandler> _requestHandlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestHandlerInvokerMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="requestHandlers">The request handlers to invoke.</param>
    public RequestHandlerInvokerMiddleware(RequestMiddlewareDelegate next, IEnumerable<IRequestHandler> requestHandlers)
    {
        _next = next;
        _requestHandlers = requestHandlers;
    }

    /// <inheritdoc />
    public async ValueTask InvokeAsync(RequestContext context)
    {
        // Find all handlers for the specified request.
        var request = context.Request;
        var requestType = request.GetType();
        var responseType = context.ResponseType;
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
        var handlers = _requestHandlers.Where(x => handlerType.IsInstanceOfType(x)).ToArray();
        
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