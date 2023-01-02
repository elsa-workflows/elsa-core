using Elsa.Workflows.Core.Pipelines.ActivityExecution;

namespace Elsa.Workflows.Core.Services;

public interface IActivityExecutionPipelineBuilder
{
    IServiceProvider ServiceProvider { get; }
    IActivityExecutionPipelineBuilder Use(Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate> middleware);
    public ActivityMiddlewareDelegate Build();
}