using Elsa.Workflows.Core.Pipelines.ActivityExecution;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Builds an activity execution pipeline.
/// </summary>
public interface IActivityExecutionPipelineBuilder
{
    /// <summary>
    /// The current service provider to resolve services from.
    /// </summary>
    IServiceProvider ServiceProvider { get; }
    
    /// <summary>
    /// Installs the specified delegate as a middleware component.
    /// </summary>
    /// <param name="middleware">The middleware delegate to install.</param>
    /// <returns>The current <see cref="IActivityExecutionPipelineBuilder"/>.</returns>
    IActivityExecutionPipelineBuilder Use(Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate> middleware);
    
    /// <summary>
    /// Constructs the final <see cref="ActivityMiddlewareDelegate"/> delegate that invokes each installed middleware component.
    /// </summary>
    /// <returns>The final <see cref="ActivityMiddlewareDelegate"/> delegate.</returns>
    public ActivityMiddlewareDelegate Build();
    
    /// <summary>
    /// Clears the current pipeline.
    /// </summary>
    IActivityExecutionPipelineBuilder Reset();
}