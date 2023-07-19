using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Workflows.Runtime.Extensions;

/// <summary>
/// Extensions for <see cref="IWorkflowStateStore"/>.
/// </summary>
public static class WorkflowStateStoreExtensions
{
    /// <summary>
    /// Finds a workflow state by ID.
    /// </summary>
    public static async Task<WorkflowState?> FindAsync(this IWorkflowStateStore store, string id, CancellationToken cancellationToken = default) =>
        await store.FindAsync(new WorkflowStateFilter { Id = id }, cancellationToken);
}