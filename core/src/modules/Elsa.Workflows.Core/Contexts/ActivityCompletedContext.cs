using System.Diagnostics.CodeAnalysis;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows;

/// <summary>
/// Provides context information when an activity has completed.
/// </summary>
public record ActivityCompletedContext(ActivityExecutionContext TargetContext, ActivityExecutionContext ChildContext, object? Result = default)
{
    /// <summary>
    /// Gets the workflow execution context.
    /// </summary>
    public WorkflowExecutionContext WorkflowExecutionContext => TargetContext.WorkflowExecutionContext;
    
    /// <summary>
    /// Resolves a required service using the service provider.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The resolved service.</returns>
    public T GetRequiredService<T>() where T : notnull => WorkflowExecutionContext.GetRequiredService<T>();
    
    /// <summary>
    /// A cancellation token to use when invoking asynchronous operations.
    /// </summary>
    public CancellationToken CancellationToken => WorkflowExecutionContext.CancellationToken;
    
    /// <summary>
    /// Complete the current activity. This should only be called by activities that explicitly suppress automatic-completion.
    /// </summary>
    [RequiresUnreferencedCode("The activity may be serialized and executed in a different context.")]
    public async ValueTask CompleteActivityAsync(object? result = default)
    {
        await TargetContext.CompleteActivityAsync(result);
    }

    /// <summary>
    /// Complete the current activity with the specified outcomes.
    /// </summary>
    [RequiresUnreferencedCode("The activity may be serialized and executed in a different context.")]
    public ValueTask CompleteActivityWithOutcomesAsync(params string[] outcomes) => CompleteActivityAsync(new Outcomes(outcomes));
}