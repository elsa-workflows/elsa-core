using CShells.Lifecycle;
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
/// The <c>DrainOrchestratorHostedService</c> registration remains so non-CShells deployments (the IModule
/// <c>Features/WorkflowRuntimeFeature</c> consumers) still drain on host stop. In CShells-hosted deployments
/// both paths can fire; the orchestrator's <c>DrainAsync</c> is idempotent — a second non-force invocation
/// throws <see cref="InvalidOperationException"/> which this handler catches and logs.
/// </para>
/// </remarks>
public sealed class ElsaShellDrainHandler(IDrainOrchestrator orchestrator, ILogger<ElsaShellDrainHandler> logger) : IDrainHandler
{
    /// <inheritdoc />
    public async Task DrainAsync(IDrainExtensionHandle extensionHandle, CancellationToken cancellationToken)
    {
        try
        {
            var outcome = await orchestrator.DrainAsync(DrainTrigger.ShellDeactivation, cancellationToken);
            if (outcome.OverallResult is DrainResult.DeadlineExceeded or DrainResult.AbortedByUnhandledException)
                logger.LogWarning("Shell drain finished with non-clean result: {Result} (forceCancelled={Count}).", outcome.OverallResult, outcome.BurstsForceCancelledCount);
            else
                logger.LogInformation("Shell drain finished: {Result} (paused={Paused}, waited={Waited}).", outcome.OverallResult, outcome.PausePhaseDuration, outcome.WaitPhaseDuration);
        }
        catch (InvalidOperationException ex)
        {
            // Another drain trigger (e.g., the host-stop hosted service) already drained this generation.
            // The orchestrator rejects parallel non-force drains with InvalidOperationException; log and swallow
            // so this handler does not block the shell platform's drain phase.
            logger.LogInformation("Shell drain skipped: {Reason}", ex.Message);
        }
    }
}
