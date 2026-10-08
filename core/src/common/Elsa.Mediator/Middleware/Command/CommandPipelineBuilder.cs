using Elsa.Mediator.Middleware.Command.Contracts;

namespace Elsa.Mediator.Middleware.Command;

/// <inheritdoc />
public class CommandPipelineBuilder : ICommandPipelineBuilder
{
    private const string ServicesKey = "mediator.Services";
    private readonly List<Func<CommandMiddlewareDelegate, CommandMiddlewareDelegate>> _components = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandPipelineBuilder"/> class.
    /// </summary>
    public CommandPipelineBuilder(IServiceProvider serviceProvider)
    {
        ApplicationServices = serviceProvider;
    }

    /// <inheritdoc />
    public IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>();

    /// <inheritdoc />
    public IServiceProvider ApplicationServices
    {
        get => GetProperty<IServiceProvider>(ServicesKey)!;
        set => SetProperty(ServicesKey, value);
    }

    /// <inheritdoc />
    public ICommandPipelineBuilder Use(Func<CommandMiddlewareDelegate, CommandMiddlewareDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }

    /// <inheritdoc />
    public ICommandPipelineBuilder Use(int index, Func<CommandMiddlewareDelegate, CommandMiddlewareDelegate> middleware)
    {
        _components.Insert(index, middleware);
        return this;
    }

    /// <inheritdoc />
    public ICommandPipelineBuilder Remove(Func<CommandMiddlewareDelegate, CommandMiddlewareDelegate> middleware)
    {
        _components.Remove(middleware);
        return this;
    }

    /// <inheritdoc />
    public ICommandPipelineBuilder RemoveAt(int index)
    {
        _components.RemoveAt(index);
        return this;
    }
    
    /// <inheritdoc />
    public ICommandPipelineBuilder Clear()
    {
        _components.Clear();
        return this;
    }

    /// <inheritdoc />
    public CommandMiddlewareDelegate Build()
    {
        CommandMiddlewareDelegate pipeline = _ => new();

        for (var i = _components.Count - 1; i >= 0; i--) 
            pipeline = _components[i](pipeline);

        return pipeline;
    }

    private T? GetProperty<T>(string key) => Properties.TryGetValue(key, out var value) ? (T?)value : default;
    private void SetProperty<T>(string key, T value) => Properties[key] = value;
}