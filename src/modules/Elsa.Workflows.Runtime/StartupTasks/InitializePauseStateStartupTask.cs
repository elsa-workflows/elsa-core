using Elsa.Common;
using Elsa.Common.RecurringTasks;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.StartupTasks;

/// <summary>
/// Startup task that re-applies any persisted administrative pause via <see cref="IQuiescenceSignal.InitializePersistedStateAsync"/>.
/// Without it, a host configured for across-reactivations pause persistence would write the persisted key on
/// pause but never read it on subsequent startups, leaving the runtime dispatching despite an operator having
/// explicitly paused it before the previous shutdown. See FR-028 / research R8.
/// </summary>
[UsedImplicitly]
[SingleNodeTask]
public sealed class InitializePauseStateStartupTask(IQuiescenceSignal signal) : IStartupTask
{
    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken cancellationToken) =>
        signal.InitializePersistedStateAsync(cancellationToken).AsTask();
}
