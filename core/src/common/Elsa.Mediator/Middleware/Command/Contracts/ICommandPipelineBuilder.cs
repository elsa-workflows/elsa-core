namespace Elsa.Mediator.Middleware.Command.Contracts;

/// <summary>
/// Represents a command pipeline builder.
/// </summary>
public interface ICommandPipelineBuilder
{
    /// <summary>
    /// Gets a property bag that can be used to share data between middleware components.
    /// </summary>
    public IDictionary<string, object?> Properties { get; }

    /// <summary>
    /// Gets the service provider.
    /// </summary>
    IServiceProvider ApplicationServices { get; }

    /// <summary>
    /// Appends a middleware component to the pipeline.
    /// </summary>
    ICommandPipelineBuilder Use(Func<CommandMiddlewareDelegate, CommandMiddlewareDelegate> middleware);
    
    /// <summary>
    /// Adds a middleware component at the specified index.
    /// </summary>
    ICommandPipelineBuilder Use(int index, Func<CommandMiddlewareDelegate, CommandMiddlewareDelegate> middleware);
    
    /// <summary>
    /// Removes a middleware component from the pipeline.
    /// </summary>
    ICommandPipelineBuilder Remove(Func<CommandMiddlewareDelegate, CommandMiddlewareDelegate> middleware);
    
    /// <summary>
    /// Removes a middleware component at the specified index from the pipeline.
    /// </summary>
    ICommandPipelineBuilder RemoveAt(int index);
    
    /// <summary>
    /// Clears the pipeline.
    /// </summary>
    ICommandPipelineBuilder Clear();

    /// <summary>
    /// Builds the pipeline.
    /// </summary>
    /// <returns>The pipeline.</returns>
    public CommandMiddlewareDelegate Build();
}