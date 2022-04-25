using Elsa.Pipelines.ActivityExecution;

namespace Elsa.Services;

public interface IActivityExecutionBuilder
{
    IServiceProvider ServiceProvider { get; }
    IActivityExecutionBuilder Use(Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate> middleware);
    public ActivityMiddlewareDelegate Build();
}