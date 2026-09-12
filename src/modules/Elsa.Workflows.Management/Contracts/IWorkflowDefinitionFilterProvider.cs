using Elsa.Authorization;
using Elsa.Workflows.Management.Filters;

namespace Elsa.Workflows.Management;

/// <summary>
/// Applies an optional workflow definition filter that is provided by another module. A provider should leave an omitted or empty criterion unchanged, clear each criterion only after successfully applying it, and report unsupported non-empty criteria explicitly.
/// </summary>
public interface IWorkflowDefinitionFilterProvider
{
    /// <summary>
    /// Gets a value indicating whether this provider can apply criteria to the specified filter.
    /// </summary>
    /// <param name="filter">The filter to inspect.</param>
    bool CanApply(WorkflowDefinitionFilter filter);

    /// <summary>
    /// Gets the permissions required before this provider can inspect data for the specified filter.
    /// </summary>
    /// <param name="filter">The filter to inspect.</param>
    IEnumerable<Permission> GetRequiredPermissions(WorkflowDefinitionFilter filter) => [];

    /// <summary>
    /// Applies the provider's criteria to the specified filter.
    /// </summary>
    /// <remarks>Criteria that remain non-empty after all providers have run are reported as unsupported by the API.</remarks>
    /// <param name="filter">The filter to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ApplyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);
}
