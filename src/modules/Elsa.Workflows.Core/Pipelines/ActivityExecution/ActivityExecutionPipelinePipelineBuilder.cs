using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Pipelines.ActivityExecution;

/// <inheritdoc />
public class ActivityExecutionPipelinePipelineBuilder : IActivityExecutionPipelineBuilder
{
    private readonly IList<Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate>> _components = new List<Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate>>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityExecutionPipelinePipelineBuilder"/> class.
    /// </summary>
    public ActivityExecutionPipelinePipelineBuilder(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; }

    /// <inheritdoc />
    public IActivityExecutionPipelineBuilder Use(Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate> middleware)
    {
        _components.Add(middleware);
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