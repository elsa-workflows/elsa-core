namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Reconciles the current tenant's workflow-as-activity descriptors with the definition store.
/// </summary>
public interface IWorkflowDefinitionActivityRegistryReconciler
{
    /// <summary>
    /// Refreshes descriptors from the authoritative definition store.
    /// </summary>
    Task ReconcileRegistryAsync(CancellationToken cancellationToken = default);
}
