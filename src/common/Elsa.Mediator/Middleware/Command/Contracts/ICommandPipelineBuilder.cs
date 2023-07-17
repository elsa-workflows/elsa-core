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
    /// Adds a middleware component to the pipeline.
    /// </summary>
    /// <param name="middleware">The middleware component.</param>
    /// <returns>The pipeline builder.</returns>
    ICommandPipelineBuilder Use(Func<CommandMiddlewareDelegate, CommandMiddlewareDelegate> middleware);

    /// <summary>
    /// Builds the pipeline.
    /// </summary>
    /// <returns>The pipeline.</returns>
    public CommandMiddlewareDelegate Build();
}