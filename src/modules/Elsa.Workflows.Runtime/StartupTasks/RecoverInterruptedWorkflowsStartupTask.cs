using Elsa.Common;
using Elsa.Common.RecurringTasks;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.StartupTasks;

/// <summary>
/// Startup task that scans for workflow instances in the <see cref="WorkflowSubStatus.Interrupted"/> sub-status
/// and requeues each one immediately, bypassing the timeout-based <c>RestartInterruptedWorkflowsTask</c> recurring
/// cadence. See FR-021 and research R4.
/// </summary>
[UsedImplicitly]
[SingleNodeTask]
public sealed class RecoverInterruptedWorkflowsStartupTask(IInterruptedRecoveryScanner scanner) : IStartupTask
{
    /// <inheritdoc />
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await scanner.ScanAndRequeueAsync(cancellationToken);
    }
}
