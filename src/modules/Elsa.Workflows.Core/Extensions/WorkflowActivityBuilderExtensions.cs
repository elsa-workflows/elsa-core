namespace Elsa.Workflows;

/// <summary>
/// Extension methods for <see cref="IActivityBuilder"/> to add workflow-related activities.
/// These are placeholder methods that demonstrate the fluent API.
/// </summary>
public static class WorkflowActivityBuilderExtensions
{
    /// <summary>
    /// Placeholder method to demonstrate RunWorkflow pattern.
    /// To use this, create extension methods that reference the actual RunWorkflow activity.
    /// </summary>
    /// <param name="builder">The activity builder.</param>
    /// <param name="workflowDefinitionId">The ID of the workflow to run.</param>
    /// <param name="input">Optional input data for the workflow.</param>
    /// <param name="outputVar">Optional variable name to store the output.</param>
    /// <returns>The activity builder for chaining.</returns>
    /// <example>
    /// <code>
    /// // Example implementation:
    /// public static IActivityBuilder RunWorkflow(this IActivityBuilder builder, string workflowDefinitionId, object? input = null, string? outputVar = null)
    /// {
    ///     return builder.Then&lt;RunTask&gt;(activity => 
    ///     {
    ///         activity.TaskName = new Input&lt;string&gt;(workflowDefinitionId);
    ///         // Configure input/output as needed
    ///     });
    /// }
    /// </code>
    /// </example>
    public static IActivityBuilder RunWorkflow(this IActivityBuilder builder, string workflowDefinitionId, object? input = null, string? outputVar = null)
    {
        throw new NotImplementedException(
            "RunWorkflow requires implementation with the actual workflow execution activity. " +
            "Add extension methods that reference the appropriate activity type from Elsa.Workflows.Runtime or similar modules.");
    }
    
    /// <summary>
    /// Placeholder method to demonstrate WaitForSignal pattern.
    /// To use this, create extension methods that reference the actual signal/event activity.
    /// </summary>
    /// <param name="builder">The activity builder.</param>
    /// <param name="signalName">The name of the signal to wait for.</param>
    /// <returns>The activity builder for chaining.</returns>
    public static IActivityBuilder WaitForSignal(this IActivityBuilder builder, string signalName)
    {
        throw new NotImplementedException(
            "WaitForSignal requires implementation with the actual event/signal activity. " +
            "Add extension methods that reference the appropriate activity type from Elsa.Workflows modules.");
    }
    
    /// <summary>
    /// Placeholder method to demonstrate Delay pattern.
    /// To use this, create extension methods that reference the Delay activity from Elsa.Scheduling.
    /// </summary>
    /// <param name="builder">The activity builder.</param>
    /// <param name="duration">The duration to delay.</param>
    /// <returns>The activity builder for chaining.</returns>
    /// <example>
    /// <code>
    /// // Example implementation (requires Elsa.Scheduling module):
    /// public static IActivityBuilder Delay(this IActivityBuilder builder, TimeSpan duration)
    /// {
    ///     return builder.Then&lt;Delay&gt;(activity => 
    ///     {
    ///         activity.TimeSpan = new Input&lt;TimeSpan&gt;(duration);
    ///     });
    /// }
    /// </code>
    /// </example>
    public static IActivityBuilder Delay(this IActivityBuilder builder, TimeSpan duration)
    {
        throw new NotImplementedException(
            "Delay requires the Elsa.Scheduling module. " +
            "Add extension methods in the Elsa.Scheduling module or your own code. " +
            "Example: builder.Then<Delay>(activity => { activity.TimeSpan = new Input<TimeSpan>(duration); })");
    }
}
