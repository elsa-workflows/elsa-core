namespace Elsa.Workflows.Runtime;

/// <summary>
/// Scans the workflow instance store for instances in the <see cref="WorkflowSubStatus.Interrupted"/> sub-status —
/// instances that were force-cancelled by a graceful drain in a prior runtime generation — and requeues each one
/// for execution. Runs once per shell activation as <c>RecoverInterruptedWorkflowsStartupTask</c>.
/// </summary>
/// <remarks>
/// Disjoint from the timeout-based <c>RestartInterruptedWorkflowsTask</c> recurring task: that task filters
/// <c>IsExecuting = true</c> with a stale <c>UpdatedAt</c>; this scan filters <see cref="WorkflowSubStatus.Interrupted"/>
/// and <see cref="WorkflowStatus.Running"/> (Interrupted instances have <c>IsExecuting = false</c>). The two
/// filters never overlap, so an instance is recovered by exactly one mechanism — see FR-022 and research R4.
/// Terminal <c>Finished+Interrupted</c> rows from a drain/runner race are excluded so they are not requeued.
/// </remarks>
public interface IInterruptedRecoveryScanner
{
    /// <summary>
    /// Enumerates running instances in the <see cref="WorkflowSubStatus.Interrupted"/> sub-status, requeues each via
    /// <c>IWorkflowRestarter</c>, and returns the count successfully requeued.
    /// </summary>
    ValueTask<int> ScanAndRequeueAsync(CancellationToken cancellationToken);
}
