using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.RecurringTasks;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Tasks;

/// <summary>
/// Reconciles the local workflow-as-activity registry with shared definition changes.
/// </summary>
[UsedImplicitly]
public class RefreshWorkflowDefinitionActivityRegistryTask(
    IWorkflowDefinitionRegistryGenerationStore generationStore,
    IWorkflowDefinitionActivityRegistryReconciler registryUpdater,
    ITenantAccessor tenantAccessor,
    TimeProvider timeProvider) : RecurringTask
{
    private static readonly TimeSpan FullReconciliationInterval = TimeSpan.FromMinutes(1);
    private long? _tenantGeneration;
    private long? _agnosticGeneration;
    private long? _lastFullReconciliationTimestamp;

    /// <inheritdoc />
    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // The default in-memory store is node-local, so polling it cannot discover remote writes.
        if (!generationStore.IsShared)
            return;

        var tenantId = tenantAccessor.TenantId ?? Tenant.DefaultTenantId;
        var tenantGeneration = await generationStore.GetGenerationAsync(tenantId, cancellationToken);
        var agnosticGeneration = await generationStore.GetGenerationAsync(Tenant.AgnosticTenantId, cancellationToken);
        var timestamp = timeProvider.GetTimestamp();
        var fullReconciliationDue = _lastFullReconciliationTimestamp == null || timeProvider.GetElapsedTime(_lastFullReconciliationTimestamp.Value, timestamp) >= FullReconciliationInterval;

        if (!fullReconciliationDue && tenantGeneration == _tenantGeneration && agnosticGeneration == _agnosticGeneration)
            return;

        // Keep the observed values from before the refresh. A concurrent write is therefore seen on the next poll.
        await registryUpdater.ReconcileRegistryAsync(cancellationToken);
        _tenantGeneration = tenantGeneration;
        _agnosticGeneration = agnosticGeneration;
        _lastFullReconciliationTimestamp = timestamp;
    }
}
