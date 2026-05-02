using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Shared invocation shell for drain triggers (host stop, shell deactivation, etc.). Calls
/// <see cref="IDrainOrchestrator.DrainAsync"/> with a uniform Information/Warning logging
/// pattern and suppresses the parallel-drain <see cref="InvalidOperationException"/> the
/// orchestrator throws when another trigger has already run for this generation.
/// </summary>
/// <remarks>
/// Both <c>ElsaShellDrainHandler</c> (CShells <c>IDrainHandler</c>) and
/// <c>DrainOrchestratorHostedService</c> (.NET <c>IHostedService.StopAsync</c>) used to inline
/// near-identical try/catch/log shapes. They had already drifted (host-stop's success log
/// omitted the <c>paused</c>/<c>waited</c> durations the shell-handler version included, and
/// the "skipped" message disagreed on the trigger label). Centralising the shape here keeps
/// the two — and any future trigger source — uniform.
/// </remarks>
internal static class DrainTriggerExecutor
{
    /// <summary>
    /// Runs a drain for the supplied trigger and logs the outcome.
    /// </summary>
    /// <param name="orchestrator">The drain orchestrator to invoke.</param>
    /// <param name="trigger">Which trigger source initiated the drain.</param>
    /// <param name="logger">Logger to emit the outcome under (the caller's category).</param>
    /// <param name="contextLabel">
    /// Human-readable label used verbatim in the log messages (e.g. <c>"Shell drain"</c>,
    /// <c>"Graceful drain"</c>) so operators can tell at a glance which trigger produced an
    /// entry without inspecting the log category.
    /// </param>
    /// <param name="cancellationToken">Cancellation propagated to the orchestrator.</param>
    public static async Task RunAsync(
        IDrainOrchestrator orchestrator,
        DrainTrigger trigger,
        ILogger logger,
        string contextLabel,
        CancellationToken cancellationToken)
    {
        try
        {
            var outcome = await orchestrator.DrainAsync(trigger, cancellationToken);
            if (outcome.OverallResult is DrainResult.DeadlineExceeded or DrainResult.AbortedByUnhandledException)
            {
                logger.LogWarning(
                    "{Context} finished with non-clean result: {Result} (forceCancelled={Count}).",
                    contextLabel, outcome.OverallResult, outcome.ExecutionCyclesForceCancelledCount);
            }
            else
            {
                logger.LogInformation(
                    "{Context} finished: {Result} (paused={Paused}, waited={Waited}).",
                    contextLabel, outcome.OverallResult, outcome.PausePhaseDuration, outcome.WaitPhaseDuration);
            }
        }
        catch (InvalidOperationException ex)
        {
            // Parallel non-force drain rejected by the orchestrator — another trigger already drained
            // this generation. Safe to swallow; the original drain's outcome stands.
            logger.LogInformation("{Context} skipped: {Reason}", contextLabel, ex.Message);
        }
    }
}
