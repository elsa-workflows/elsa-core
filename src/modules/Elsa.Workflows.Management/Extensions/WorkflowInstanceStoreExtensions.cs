using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides a set of extension methods for <see cref="IWorkflowInstanceStore"/>.
/// </summary>
public static class WorkflowInstanceStoreExtensions
{
    /// <summary>
    /// Finds a workflow instance matching the specified ID.
    /// </summary>
    public static async ValueTask<WorkflowInstance?> FindAsync(this IWorkflowInstanceStore store, string id, CancellationToken cancellationToken = default)
    {
        return await store.FindAsync(new WorkflowInstanceFilter{ Id = id }, cancellationToken);
    }
}