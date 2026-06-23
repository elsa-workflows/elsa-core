using System.Transactions;
using Elsa.Workflows.Runtime;

namespace Elsa.Persistence.EFCore.Modules.Runtime;

/// <summary>
/// Wraps workflow commit persistence operations in an ambient transaction so EF Core stores enlist automatically.
/// </summary>
public class EFCoreWorkflowCommitTransaction : IWorkflowCommitTransaction
{
    /// <inheritdoc />
    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        var transactionOptions = new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TransactionManager.MaximumTimeout
        };

        using var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled);
        await operation(cancellationToken);
        scope.Complete();
    }
}
