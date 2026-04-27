using Elsa.Common;
using Elsa.Common.RecurringTasks;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.StartupTasks;

/// <summary>
/// Startup task that re-applies any persisted administrative pause when <see cref="GracefulShutdownOptions.PausePersistence"/>
/// is set to <see cref="PausePersistencePolicy.AcrossReactivations"/>. Without this task, a host configured for
/// across-reactivations pause persistence would write the persisted key on pause but never read it on subsequent
/// startups — meaning the runtime would resume dispatching even though an operator had explicitly paused it before
/// the previous shutdown. See FR-028 / research R8.
/// </summary>
[UsedImplicitly]
[SingleNodeTask]
public sealed class InitializePauseStateStartupTask(
    IQuiescenceSignal signal,
    IOptions<GracefulShutdownOptions> options) : IStartupTask
{
    /// <inheritdoc />
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (options.Value.PausePersistence != PausePersistencePolicy.AcrossReactivations) return;

        if (signal is QuiescenceSignal concrete)
            await concrete.InitializePersistedStateAsync(cancellationToken);
    }
}
