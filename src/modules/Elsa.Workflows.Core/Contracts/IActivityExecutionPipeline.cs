using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;

namespace Elsa.Workflows.Core.Contracts;

public interface IActivityExecutionPipeline
{
    ActivityMiddlewareDelegate Setup(Action<IActivityExecutionPipelineBuilder> setup);
    ActivityMiddlewareDelegate Pipeline { get; }
    Task ExecuteAsync(ActivityExecutionContext context);
}