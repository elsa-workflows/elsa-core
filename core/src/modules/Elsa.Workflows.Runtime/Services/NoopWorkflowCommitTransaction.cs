namespace Elsa.Workflows.Runtime;

/// <summary>
/// Default transaction strategy for stores that do not expose a shared transaction boundary.
/// </summary>
public class NoopWorkflowCommitTransaction : IWorkflowCommitTransaction
{
    /// <inheritdoc />
    public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        return operation(cancellationToken);
    }
}
