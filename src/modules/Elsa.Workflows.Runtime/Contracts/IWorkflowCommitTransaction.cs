namespace Elsa.Workflows.Runtime;

/// <summary>
/// Executes workflow commit persistence operations inside an optional transaction boundary.
/// </summary>
public interface IWorkflowCommitTransaction
{
    /// <summary>
    /// Executes the specified operation.
    /// </summary>
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}
