using CShells.Lifecycle;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Lifecycle;

/// <summary>
/// CShells <see cref="IDrainHandler"/> that bridges per-shell drain into the workflow runtime's
/// <see cref="IDrainOrchestrator"/>. The shell platform invokes this when a shell enters
/// <see cref="ShellLifecycleState.Draining"/> (e.g., on shell reload or host shutdown). The drain handler's
/// <see cref="CancellationToken"/> is signalled when the CShells drain deadline elapses, so the orchestrator's
/// own deadline-bounded protocol nests cleanly inside the per-shell deadline policy.
/// </summary>
/// <remarks>
/// <para>
/// Registered as transient via <c>IShellFeature.ConfigureServices</c>; CShells resolves all
/// <see cref="IDrainHandler"/> implementations from the shell's <see cref="IServiceProvider"/> at draining time
/// and invokes them in parallel. This gives FR-027 ("a shell moving into its deactivation phase drains that
/// shell's runtime, scoped so sibling shells are unaffected") a first-class, per-shell mechanism — replacing
/// the earlier R3 design that relied on <c>IHostApplicationLifetime.ApplicationStopping</c>.
/// </para>
/// <para>
/// In CShells-hosted deployments this is the sole drain trigger: host stop reaches the runtime via CShells's
/// shell-drain pipeline (<c>CShellsStartupHostedService.StopAsync</c>), not via a host-level
/// <see cref="IHostedService"/>. The host-stop <c>DrainOrchestratorHostedService</c> registration only lives on
/// the IModule <c>Features/WorkflowRuntimeFeature</c> path, where there is no shell platform.
/// </para>
/// </remarks>
public sealed class ElsaShellDrainHandler(IDrainOrchestrator orchestrator, ILogger<ElsaShellDrainHandler> logger) : IDrainHandler
{
    /// <inheritdoc />
    public Task DrainAsync(IDrainExtensionHandle extensionHandle, CancellationToken cancellationToken) =>
        DrainTriggerExecutor.RunAsync(orchestrator, DrainTrigger.ShellDeactivation, logger, "Shell drain", cancellationToken);
}
