using Elsa.Models;
using Elsa.Pipelines.ActivityExecution;

namespace Elsa.Services;

public interface IActivityExecutionPipeline
{
    ActivityMiddlewareDelegate Setup(Action<IActivityExecutionBuilder> setup);
    ActivityMiddlewareDelegate Pipeline { get; }
    Task ExecuteAsync(ActivityExecutionContext context);
}