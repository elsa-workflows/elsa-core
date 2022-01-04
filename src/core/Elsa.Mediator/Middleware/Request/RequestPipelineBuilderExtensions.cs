using Elsa.Mediator.Middleware.Request.Components;
using Elsa.Mediator.Middleware.Request.Contracts;

namespace Elsa.Mediator.Middleware.Request;

public static class RequestPipelineBuilderExtensions
{
    public static IRequestPipelineBuilder UseRequestHandlers(this IRequestPipelineBuilder builder) => builder.UseMiddleware<RequestHandlerInvokerMiddleware>();
    public static IRequestPipelineBuilder UseRequestLogging(this IRequestPipelineBuilder builder) => builder.UseMiddleware<RequestLoggingMiddleware>();
}