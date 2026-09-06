using Elsa.Workflows.Management.Filters;

namespace Elsa.Workflows.Management;

/// <summary>
/// Applies an optional workflow definition filter that is provided by another module. A provider should leave an omitted or empty criterion unchanged and report unsupported non-empty criteria explicitly.
/// </summary>
public interface IWorkflowDefinitionFilterProvider
{
    /// <summary>
    /// Gets a value indicating whether this provider can apply criteria to the specified filter.
    /// </summary>
    /// <param name="filter">The filter to inspect.</param>
    bool CanApply(WorkflowDefinitionFilter filter);

    /// <summary>
    /// Applies the provider's criteria to the specified filter.
    /// </summary>
    /// <param name="filter">The filter to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ApplyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);
}
