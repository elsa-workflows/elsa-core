using Elsa.Mediator.PublishingStrategies;

namespace Elsa.Mediator;

/// <summary>
/// Provides a set of strategies for publishing events.
/// </summary>
public static class NotificationStrategy
{
    /// <summary>
    /// Invokes event handlers in parallel and waits for the result.
    /// </summary>
    public static readonly FireAndForgetStrategy FireAndForget = new();
    
    /// <summary>
    /// Invokes event handlers in the background and does not wait for the result.
    /// </summary>
    public static readonly BackgroundProcessingStrategy Background = new();
    
    /// <summary>
    /// Invokes event handlers in parallel and waits for the result.
    /// </summary>
    public static readonly ParallelProcessingStrategy Parallel = new();
    
    /// <summary>
    /// Invokes event handlers in sequence and waits for the result.
    /// </summary>
    public static readonly SequentialProcessingStrategy Sequential = new();
}