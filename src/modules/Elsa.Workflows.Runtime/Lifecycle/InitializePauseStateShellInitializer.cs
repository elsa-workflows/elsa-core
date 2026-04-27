using CShells.Lifecycle;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Lifecycle;

/// <summary>
/// CShells <see cref="IShellInitializer"/> that re-applies any persisted administrative pause state when a shell
/// is activated. Without this hook, a shell configured for <see cref="PausePersistencePolicy.AcrossReactivations"/>
/// pause persistence would write the persisted key on pause but never read it on subsequent reactivations — meaning
/// the runtime would resume dispatching even though an operator had explicitly paused it before the previous
/// shell tear-down.
/// </summary>
/// <remarks>
/// <para>
/// This is the shell-aware counterpart of <c>InitializePauseStateStartupTask</c> (used by IModule consumers).
/// Both call <see cref="QuiescenceSignal.InitializePersistedStateAsync"/>, but they fire at different lifecycle
/// points:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///     <c>IStartupTask</c> runs once when the IModule host starts up. Correct for non-shell deployments,
///     but in a shell-aware deployment a shell can be activated and reactivated independently of the host —
///     the startup task only fires for the FIRST activation.
///     </description>
///   </item>
///   <item>
///     <description>
///     <see cref="IShellInitializer"/> runs on EVERY shell activation, including reactivations after a reload.
///     This is what FR-028 requires: pause state survives every shell reactivation, not just the first.
///     </description>
///   </item>
/// </list>
/// </remarks>
public sealed class InitializePauseStateShellInitializer(IQuiescenceSignal signal) : IShellInitializer
{
    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        signal.InitializePersistedStateAsync(cancellationToken).AsTask();
}
