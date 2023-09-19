namespace Elsa.Workflows.Core;

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
    public CancellationToken CancellationToken => WorkflowExecutionContext.CancellationTokens.ApplicationCancellationToken;
}