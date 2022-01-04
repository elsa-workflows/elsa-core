using Elsa.Mediator.Middleware.Request.Contracts;

namespace Elsa.Mediator.Middleware.Request;

public class RequestPipeline : IRequestPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private RequestMiddlewareDelegate? _pipeline;

    public RequestPipeline(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
    public RequestMiddlewareDelegate Pipeline => _pipeline ??= CreateDefaultPipeline();

    public RequestMiddlewareDelegate Setup(Action<IRequestPipelineBuilder>? setup = default)
    {
        var builder = new RequestPipelineBuilder(_serviceProvider);
        setup?.Invoke(builder);
        _pipeline = builder.Build();
        return _pipeline;
    }

    public async Task ExecuteAsync(RequestContext context) => await Pipeline(context);

    private RequestMiddlewareDelegate CreateDefaultPipeline() => Setup(x => x.UseRequestHandlers());
}