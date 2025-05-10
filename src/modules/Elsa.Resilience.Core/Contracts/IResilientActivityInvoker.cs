using Elsa.Workflows;

namespace Elsa.Resilience;

/// <summary>
/// Provides functionality to execute an activity's logic with a specified resilience strategy, or execute the action directly if no resilience configuration is applicable.
/// </summary>
public interface IResilientActivityInvoker
{
    /// <summary>
    /// Invokes a resilient activity execution with the provided action using the activity's selected resilience strategy configuration.
    /// If no resilience configuration is provided or otherwise does not evaluate to a strategy, the action is called as-is.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the activity.</typeparam>
    /// <param name="activity">The resilient activity being invoked.</param>
    /// <param name="context">The execution context in which the activity runs.</param>
    /// <param name="action">The action representing the activity's execution logic.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The result of the invoked action.</returns>
    Task<T> InvokeAsync<T>(IResilientActivity activity, ActivityExecutionContext context, Func<Task<T>> action, CancellationToken cancellationToken = default);
}