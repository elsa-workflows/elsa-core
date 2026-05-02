namespace Elsa.Workflows.Runtime;

/// <summary>
/// Owns the drain protocol end-to-end: parallel pause of every <see cref="IIngressSource"/>, wait for active execution cycles to
/// reach zero within the drain deadline, force-cancel + mark <see cref="WorkflowSubStatus.Interrupted"/> on breach,
/// emit the forensic <c>WorkflowInterrupted</c> event per affected instance, return a <see cref="DrainOutcome"/>.
/// See <c>contracts/drain-orchestrator.md</c> for the full protocol.
/// </summary>
public interface IDrainOrchestrator
{
    /// <summary>
    /// Drains the runtime under the supplied trigger and returns the structured outcome.
    /// MUST be called at most once per generation for non-force triggers; subsequent calls throw.
    /// Force-trigger calls during an in-progress non-force drain return immediately with the previous outcome.
    /// </summary>
    ValueTask<DrainOutcome> DrainAsync(DrainTrigger trigger, CancellationToken cancellationToken = default);
}
