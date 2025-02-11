namespace Elsa.Workflows;

/// <summary>
/// The interface for activity execution middleware components.
/// </summary>
public interface IActivityExecutionMiddleware
{
    /// <summary>
    /// The method that is called to execute the middleware.
    /// </summary>
    ValueTask InvokeAsync(ActivityExecutionContext context);
}

