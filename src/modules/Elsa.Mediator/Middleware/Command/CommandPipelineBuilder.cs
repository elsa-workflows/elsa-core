using Elsa.Mediator.Middleware.Command.Contracts;

namespace Elsa.Mediator.Middleware.Command;

public class CommandPipelineBuilder : ICommandPipelineBuilder
{
    private const string ServicesKey = "mediator.Services";
    private readonly IList<Func<CommandMiddlewareDelegate, CommandMiddlewareDelegate>> _components = new List<Func<CommandMiddlewareDelegate, CommandMiddlewareDelegate>>();

    public CommandPipelineBuilder(IServiceProvider serviceProvider)
    {
        ApplicationServices = serviceProvider;
    }

    public IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>();

    public IServiceProvider ApplicationServices
    {
        get => GetProperty<IServiceProvider>(ServicesKey)!;
        set => SetProperty(ServicesKey, value);
    }

    public ICommandPipelineBuilder Use(Func<CommandMiddlewareDelegate, CommandMiddlewareDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }
        
    public CommandMiddlewareDelegate Build()
    {
        CommandMiddlewareDelegate pipeline = _ => new ValueTask();

        foreach (var component in _components.Reverse()) 
            pipeline = component(pipeline);

        return pipeline;
    }

    private T? GetProperty<T>(string key) => Properties.TryGetValue(key, out var value) ? (T?)value : default(T);
    private void SetProperty<T>(string key, T value) => Properties[key] = value;
}