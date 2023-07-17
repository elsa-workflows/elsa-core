namespace Elsa.Mediator.Middleware.Request.Contracts;

/// <summary>
/// Represents a request pipeline. A request pipeline is a chain of middleware that can be used to process a request.
/// </summary>
public interface IRequestPipeline
{
    /// <summary>
    /// Sets up the request pipeline.
    /// </summary>
    /// <param name="setup">The setup action.</param>
    /// <returns>A delegate representing the request pipeline.</returns>
    RequestMiddlewareDelegate Setup(Action<IRequestPipelineBuilder> setup);
    
    
    /// <summary>
    /// Gets the delegate representing the request pipeline.
    /// </summary>
    RequestMiddlewareDelegate Pipeline { get; }
    
    /// <summary>
    /// Executes the request pipeline.
    /// </summary>
    /// <param name="context">The request context.</param>
    Task ExecuteAsync(RequestContext context);
}