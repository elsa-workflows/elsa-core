using Elsa.Api.Client.Resources.WorkflowExecutionContexts.Models;

namespace Elsa.Studio.WorkflowContexts.Contracts;

/// <summary>
/// Provides workflow contexts.
/// </summary>
public interface IWorkflowContextsProvider
{
    /// <summary>
    /// Returns a list of workflow context provider descriptors.
    /// </summary>
    Task<IEnumerable<WorkflowContextProviderDescriptor>> ListAsync(CancellationToken cancellationToken = default);
}