using Elsa.Mediator.Middleware.Request.Contracts;

namespace Elsa.Mediator.Middleware.Request;

public class RequestPipelineBuilder : IRequestPipelineBuilder
{
    private const string ServicesKey = "mediator.Services";
    private readonly IList<Func<RequestMiddlewareDelegate, RequestMiddlewareDelegate>> _components = new List<Func<RequestMiddlewareDelegate, RequestMiddlewareDelegate>>();

    public RequestPipelineBuilder(IServiceProvider serviceProvider)
    {
        ApplicationServices = serviceProvider;
    }

    public IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>();

    public IServiceProvider ApplicationServices
    {
        get => GetProperty<IServiceProvider>(ServicesKey)!;
        set => SetProperty(ServicesKey, value);
    }

    public IRequestPipelineBuilder Use(Func<RequestMiddlewareDelegate, RequestMiddlewareDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }
        
    public RequestMiddlewareDelegate Build()
    {
        RequestMiddlewareDelegate pipeline = _ => new ValueTask();

        foreach (var component in _components.Reverse()) 
            pipeline = component(pipeline);

        return pipeline;
    }

    private T? GetProperty<T>(string key) => Properties.TryGetValue(key, out var value) ? (T?)value : default(T);
    private void SetProperty<T>(string key, T value) => Properties[key] = value;
}