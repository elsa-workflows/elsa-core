using Elsa.Common;
using Elsa.Common.RecurringTasks;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.StartupTasks;

/// <summary>
/// Startup task that scans for running workflow instances in the <see cref="WorkflowSubStatus.Interrupted"/> sub-status
/// and requeues each one immediately, bypassing the timeout-based <c>RestartInterruptedWorkflowsTask</c> recurring
/// cadence. See FR-021 and research R4. Terminal Finished+Interrupted rows are left untouched.
/// It runs once after all tenants are activated through <c>RecoverInterruptedWorkflowsOnAllTenantsActivated</c> and must not
/// be registered as a per-tenant startup task, because its scan is tenant-agnostic.
/// </summary>
[UsedImplicitly]
[SingleNodeTask(SingleNodeTaskScope.Host)]
public sealed class RecoverInterruptedWorkflowsStartupTask(IInterruptedRecoveryScanner scanner) : IStartupTask
{
    /// <inheritdoc />
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await scanner.ScanAndRequeueAsync(cancellationToken);
    }
}
