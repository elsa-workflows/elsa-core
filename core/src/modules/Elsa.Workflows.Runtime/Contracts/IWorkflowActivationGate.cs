using Elsa.Workflows.Activities;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Serializes activation-strategy evaluation with instance creation so concurrent starts cannot
/// both observe "no running instance" and both create one.
/// </summary>
public interface IWorkflowActivationGate
{
    /// <summary>
    /// Acquires the strategy-scoped lock (when the workflow has an activation strategy) and
    /// evaluates whether a new instance may be created. Dispose the lease after the new instance
    /// has been persisted so later checkers observe it.
    /// </summary>
    Task<WorkflowActivationLease> EvaluateAsync(Workflow workflow, string? correlationId, CancellationToken cancellationToken = default);
}
