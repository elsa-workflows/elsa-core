using Elsa.Common;
using Elsa.Common.RecurringTasks;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Distributed.StartupTasks;

/// <summary>
/// Fails startup when distributed runtime uses a local-only lock provider without explicit opt-in.
/// </summary>
[UsedImplicitly]
[Order(-1000)]
public class ValidateDistributedRuntimeLockProviderStartupTask(DistributedRuntimeLockProviderValidator validator) : IStartupTask
{
    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        validator.Validate();
        return Task.CompletedTask;
    }
}
