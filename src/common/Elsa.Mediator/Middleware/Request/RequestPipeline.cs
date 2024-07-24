using Elsa.Mediator.Middleware.Request.Contracts;

namespace Elsa.Mediator.Middleware.Request;

/// <inheritdoc />
/// <summary>
/// Initializes a new instance of the <see cref="RequestPipeline"/> class.
/// </summary>
public class RequestPipeline(IServiceProvider serviceProvider) : IRequestPipeline
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private RequestMiddlewareDelegate? _pipeline;

    /// <inheritdoc />
    public RequestMiddlewareDelegate Pipeline => _pipeline ??= CreateDefaultPipeline();

    /// <inheritdoc />
    public RequestMiddlewareDelegate Setup(Action<IRequestPipelineBuilder>? setup = default)
    {
        var builder = new RequestPipelineBuilder(_serviceProvider);
        setup?.Invoke(builder);
        _pipeline = builder.Build();
        return _pipeline;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(RequestContext context) => await Pipeline(context);

    private RequestMiddlewareDelegate CreateDefaultPipeline() => Setup(x => x.UseRequestHandlers());
}