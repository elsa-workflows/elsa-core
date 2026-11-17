namespace Elsa.Workflows;

/// <summary>
/// Claims a new instance identity by inserting the prepared execution into its instance store.
/// </summary>
public interface IWorkflowStartPersistence
{
    /// <summary>
    /// Persists the initial, restartable execution state. Implementations must reject an occupied ID
    /// atomically and leave the existing instance untouched.
    /// </summary>
    Task CreateAsync(WorkflowExecutionContext context, CancellationToken cancellationToken);
}
