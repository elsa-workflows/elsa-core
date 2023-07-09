using Elsa.Workflows.Core.Pipelines.ActivityExecution;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Represents a pipeline that can be used to execute an activity.
/// </summary>
public interface IActivityExecutionPipeline
{
    /// <summary>
    /// Sets up the pipeline.
    /// </summary>
    /// <param name="setup">The callback that configures the pipeline.</param>
    /// <returns>The final <see cref="ActivityMiddlewareDelegate"/> delegate.</returns>
    ActivityMiddlewareDelegate Setup(Action<IActivityExecutionPipelineBuilder> setup);
    
    /// <summary>
    /// The pipeline delegate.
    /// </summary>
    ActivityMiddlewareDelegate Pipeline { get; }
    
    /// <summary>
    /// Executes the pipeline.
    /// </summary>
    /// <param name="context">The activity execution context.</param>
    Task ExecuteAsync(ActivityExecutionContext context);
}