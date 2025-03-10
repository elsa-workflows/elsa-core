namespace Elsa.Workflows.Pipelines.ActivityExecution;

/// <inheritdoc />
public class ActivityExecutionPipelinePipelineBuilder(IServiceProvider serviceProvider) : IActivityExecutionPipelineBuilder
{
    private readonly IList<Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate>> _components = new List<Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate>>();

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    /// <inheritdoc />
    public IActivityExecutionPipelineBuilder Use(Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }

    /// <inheritdoc />
    public IActivityExecutionPipelineBuilder Insert(int index, Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate> middleware)
    {
        _components.Insert(index, middleware);
        return this;
    }

    /// <inheritdoc />
    public ActivityMiddlewareDelegate Build()
    {
        ActivityMiddlewareDelegate pipeline = _ => new ValueTask();

        foreach (var component in _components.Reverse()) 
            pipeline = component(pipeline);

        return pipeline;
    }

    /// <inheritdoc />
    public IActivityExecutionPipelineBuilder Reset()
    {
        _components.Clear();
        return this;
    }
}