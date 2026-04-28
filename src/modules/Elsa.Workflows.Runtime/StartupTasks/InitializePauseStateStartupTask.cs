using Elsa.Common;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.StartupTasks;

/// <summary>
/// Startup task that re-applies any persisted administrative pause via <see cref="IQuiescenceSignal.InitializePersistedStateAsync"/>.
/// Without it, a host configured for across-reactivations pause persistence would write the persisted key on
/// pause but never read it on subsequent startups, leaving the runtime dispatching despite an operator having
/// explicitly paused it before the previous shutdown. See FR-028 / research R8.
/// </summary>
/// <remarks>
/// This task MUST run on every node — it deliberately does NOT carry <c>[SingleNodeTask]</c>.
/// <see cref="IQuiescenceSignal"/> is registered as a singleton in each node's DI container, so each node holds
/// its own in-memory <see cref="QuiescenceState"/>. Gating the task to a single cluster winner would leave every
/// other node starting with <see cref="QuiescenceReason.None"/> and dispatching new work, silently defeating the
/// persisted pause. The per-shell counterpart for shell-aware deployments is
/// <c>InitializePauseStateShellInitializer</c>, which fires per-shell (and therefore per-node) for the same reason.
/// </remarks>
[UsedImplicitly]
public sealed class InitializePauseStateStartupTask(IQuiescenceSignal signal) : IStartupTask
{
    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken cancellationToken) =>
        signal.InitializePersistedStateAsync(cancellationToken).AsTask();
}
