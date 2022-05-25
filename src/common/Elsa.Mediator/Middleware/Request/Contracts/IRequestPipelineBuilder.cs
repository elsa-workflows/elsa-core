namespace Elsa.Mediator.Middleware.Request.Contracts;

public interface IRequestPipelineBuilder
{
    public IDictionary<string, object?> Properties { get; }
    IServiceProvider ApplicationServices { get; }
    IRequestPipelineBuilder Use(Func<RequestMiddlewareDelegate, RequestMiddlewareDelegate> middleware);
    public RequestMiddlewareDelegate Build();
}