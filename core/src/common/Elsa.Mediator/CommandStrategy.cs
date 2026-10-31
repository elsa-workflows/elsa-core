using Elsa.Mediator.CommandStrategies;

namespace Elsa.Mediator;

/// <summary>
/// Provides a set of strategies for publishing events.
/// </summary>
public static class CommandStrategy
{
    /// <summary>
    /// Invokes command handlers in parallel and waits for the result.
    /// </summary>
    public static readonly DefaultStrategy Default = new();
    
    /// <summary>
    /// Invokes command handlers in the background and does not wait for the result.
    /// </summary>
    public static readonly BackgroundStrategy Background = new();
}