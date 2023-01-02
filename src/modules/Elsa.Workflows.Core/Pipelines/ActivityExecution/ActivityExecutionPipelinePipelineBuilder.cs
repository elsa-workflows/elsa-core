using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Pipelines.ActivityExecution;

public class ActivityExecutionPipelinePipelineBuilder : IActivityExecutionPipelineBuilder
{
    private readonly IList<Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate>> _components = new List<Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate>>();

    public ActivityExecutionPipelinePipelineBuilder(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
        
    public IServiceProvider ServiceProvider { get; }

    public IActivityExecutionPipelineBuilder Use(Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate> middleware)
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