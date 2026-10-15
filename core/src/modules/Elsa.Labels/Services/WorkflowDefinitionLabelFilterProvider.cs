using Elsa.Authorization;
using Elsa.Labels.Contracts;
using Elsa.Labels.Permissions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;

namespace Elsa.Labels.Services;

/// <summary>
/// Applies label relationships to workflow definition filters. A version qualifies when it has any requested label ID; an unknown label produces no matches. A caller filtering by labels must hold the workflow-definition-label view permission. The configured label store must implement <see cref="IWorkflowDefinitionLabelQuery"/> to support this filter.
/// </summary>
public class WorkflowDefinitionLabelFilterProvider(IWorkflowDefinitionLabelStore store) : IWorkflowDefinitionFilterProvider
{
    /// <inheritdoc />
    public bool CanApply(WorkflowDefinitionFilter filter) => filter.LabelIds is { Count: > 0 };

    /// <summary>Requires workflow-definition-label view permission when a label filter is present.</summary>
    public IEnumerable<Permission> GetRequiredPermissions(WorkflowDefinitionFilter filter) =>
        CanApply(filter) ? [new(LabelPermissions.WorkflowDefinitionLabels, CoreVerbs.View)] : [];

    /// <inheritdoc />
    public async Task ApplyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        if (filter.LabelIds is not { Count: > 0 })
        {
            return;
        }

        if (store is not IWorkflowDefinitionLabelQuery query)
        {
            throw new WorkflowDefinitionFilterNotSupportedException("Filtering workflow definitions by labels is not supported by the configured label store.");
        }

        var labelIds = filter.LabelIds.Distinct().ToList();
        var matchingVersionIds = (await query.FindByLabelIdsAsync(labelIds, cancellationToken))
            .Select(x => x.WorkflowDefinitionVersionId)
            .Distinct()
            .ToList();

        filter.Ids = filter.Ids == null
            ? matchingVersionIds
            : filter.Ids.Intersect(matchingVersionIds).ToList();
        filter.LabelIds = null;
    }
}
