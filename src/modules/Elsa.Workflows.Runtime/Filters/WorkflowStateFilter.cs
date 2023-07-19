using Elsa.Workflows.Core;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Runtime.Filters;

/// <summary>
/// A specification for querying workflow states.
/// </summary>
public class WorkflowStateFilter
{
    /// <summary>
    /// Filter workflow states by ID.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Filter workflow states by IDs.
    /// </summary>
    public ICollection<string>? Ids { get; set; }

    /// <summary>
    /// Filter workflow states by definition ID.
    /// </summary>
    public string? DefinitionId { get; set; }

    /// <summary>
    /// Filter workflow states by definition version ID.
    /// </summary>
    public string? DefinitionVersionId { get; set; }

    /// <summary>
    /// Filter workflow states by definition IDs.
    /// </summary>
    public ICollection<string>? DefinitionIds { get; set; }

    /// <summary>
    /// Filter workflow states by definition version IDs.
    /// </summary>
    public ICollection<string>? DefinitionVersionIds { get; set; }

    /// <summary>
    /// Filter workflow states by version.
    /// </summary>
    public int? Version { get; set; }

    /// <summary>
    /// Filter workflow states by correlation ID.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Filter workflow states by correlation IDs.
    /// </summary>
    public ICollection<string>? CorrelationIds { get; set; }

    /// <summary>
    /// Filter workflow states by status.
    /// </summary>
    public WorkflowStatus? WorkflowStatus { get; set; }

    /// <summary>
    /// Filter workflow states by a set of statuses.
    /// </summary>
    public ICollection<WorkflowStatus>? WorkflowStatuses { get; set; }

    /// <summary>
    /// Filter workflow states by sub-status.
    /// </summary>
    public WorkflowSubStatus? WorkflowSubStatus { get; set; }

    /// <summary>
    /// Filter workflow states by a set of sub-status.
    /// </summary>
    public ICollection<WorkflowSubStatus>? WorkflowSubStatuses { get; set; }

    /// <summary>
    /// Applies the filter to the specified query.
    /// </summary>
    /// <param name="query">The query to apply the filter to.</param>
    /// <returns>The filtered query.</returns>
    public IQueryable<WorkflowState> Apply(IQueryable<WorkflowState> query)
    {
        var filter = this;
        if (!string.IsNullOrWhiteSpace(filter.Id)) query = query.Where(x => x.Id == filter.Id);
        if (filter.Ids != null) query = query.Where(x => filter.Ids.Contains(x.Id));
        if (!string.IsNullOrWhiteSpace(filter.DefinitionId)) query = query.Where(x => x.DefinitionId == filter.DefinitionId);
        if (!string.IsNullOrWhiteSpace(filter.DefinitionVersionId)) query = query.Where(x => x.DefinitionVersionId == filter.DefinitionVersionId);
        if (filter.DefinitionIds != null) query = query.Where(x => filter.DefinitionIds.Contains(x.DefinitionId));
        if (filter.DefinitionVersionIds != null) query = query.Where(x => filter.DefinitionVersionIds.Contains(x.DefinitionVersionId));
        if (filter.Version != null) query = query.Where(x => x.DefinitionVersion == filter.Version);
        if (!string.IsNullOrWhiteSpace(filter.CorrelationId)) query = query.Where(x => x.CorrelationId == filter.CorrelationId);
        if (filter.CorrelationIds != null) query = query.Where(x => filter.CorrelationIds.Contains(x.CorrelationId!));
        if (filter.WorkflowStatus != null) query = query.Where(x => x.Status == filter.WorkflowStatus);
        if (filter.WorkflowSubStatus != null) query = query.Where(x => x.SubStatus == filter.WorkflowSubStatus);
        if (filter.WorkflowStatuses != null) query = query.Where(x => filter.WorkflowStatuses.Contains(x.Status));
        if (filter.WorkflowSubStatuses != null) query = query.Where(x => filter.WorkflowSubStatuses.Contains(x.SubStatus));

        return query;
    }
}