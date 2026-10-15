namespace Elsa.Mediator.Middleware.Command.Contracts;

/// <summary>
/// Represents a pipeline for processing commands. The pipeline is responsible for orchestrating the execution of registered middleware in sequence.
/// </summary>
public interface ICommandPipeline
{
    /// <summary>
    /// Configures the pipeline.
    /// </summary>
    /// <param name="setup">A delegate that configures the pipeline.</param>
    /// <returns>The pipeline.</returns>
    CommandMiddlewareDelegate Setup(Action<ICommandPipelineBuilder> setup);
    
    /// <summary>
    /// Gets the pipeline.
    /// </summary>
    CommandMiddlewareDelegate Pipeline { get; }
    
    /// <summary>
    /// Invokes the pipeline.
    /// </summary>
    /// <param name="context">The command context.</param>
    Task InvokeAsync(CommandContext context);
}