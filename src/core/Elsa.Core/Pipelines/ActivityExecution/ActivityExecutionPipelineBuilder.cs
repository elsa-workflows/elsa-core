using Elsa.Contracts;

namespace Elsa.Pipelines.ActivityExecution;

public class ActivityExecutionPipelineBuilder : IActivityExecutionBuilder
{
    private readonly IList<Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate>> _components = new List<Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate>>();

    public ActivityExecutionPipelineBuilder(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
        
    public IServiceProvider ServiceProvider { get; }

    public IActivityExecutionBuilder Use(Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }
        
    public ActivityMiddlewareDelegate Build()
    {
        ActivityMiddlewareDelegate pipeline = _ => new ValueTask();

        foreach (var component in _components.Reverse()) 
            pipeline = component(pipeline);

        return pipeline;
    }
}