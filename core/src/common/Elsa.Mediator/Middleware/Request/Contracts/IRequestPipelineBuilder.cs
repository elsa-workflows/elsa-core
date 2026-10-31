namespace Elsa.Mediator.Middleware.Request.Contracts;

/// <summary>
/// Represents a builder for building a request pipeline.
/// </summary>
public interface IRequestPipelineBuilder
{
    /// <summary>
    /// Gets the properties associated with the request pipeline.
    /// </summary>
    public IDictionary<string, object?> Properties { get; }
    
    /// <summary>
    /// Gets the service provider.
    /// </summary>
    IServiceProvider ApplicationServices { get; }
    
    /// <summary>
    /// Adds a middleware to the request pipeline.
    /// </summary>
    /// <param name="middleware">The middleware to add.</param>
    IRequestPipelineBuilder Use(Func<RequestMiddlewareDelegate, RequestMiddlewareDelegate> middleware);
    
    /// <summary>
    /// Builds the request pipeline.
    /// </summary>
    /// <returns></returns>
    public RequestMiddlewareDelegate Build();
}