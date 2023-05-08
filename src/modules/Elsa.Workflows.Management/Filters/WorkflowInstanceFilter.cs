using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Filters;

/// <summary>
/// A filter for querying workflow instances.
/// </summary>
public class WorkflowInstanceFilter
{
    /// <summary>
    /// Filter workflow instances by ID.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Filter workflow instances by IDs.
    /// </summary>
    public ICollection<string>? Ids { get; set; }

    /// <summary>
    /// Filter workflow instances that match the specified search term.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter workflow instances by definition ID.
    /// </summary>
    public string? DefinitionId { get; set; }

    /// <summary>
    /// Filter workflow instances by definition version ID.
    /// </summary>
    public string? DefinitionVersionId { get; set; }

    /// <summary>
    /// Filter workflow instances by definition IDs.
    /// </summary>
    public ICollection<string>? DefinitionIds { get; set; }

    /// <summary>
    /// Filter workflow instances by definition version IDs.
    /// </summary>
    public ICollection<string>? DefinitionVersionIds { get; set; }

    /// <summary>
    /// Filter workflow instances by version.
    /// </summary>
    public int? Version { get; set; }

    /// <summary>
    /// Filter workflow instances by correlation ID.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Filter workflow instances by correlation IDs.
    /// </summary>
    public ICollection<string>? CorrelationIds { get; set; }

    /// <summary>
    /// Filter workflow instances by status.
    /// </summary>
    public WorkflowStatus? WorkflowStatus { get; set; }

    /// <summary>
    /// Filter workflow instances by sub-status.
    /// </summary>
    public WorkflowSubStatus? WorkflowSubStatus { get; set; }

    /// <summary>
    /// Applies the filter to the specified query.
    /// </summary>
    /// <param name="query">The query to apply the filter to.</param>
    /// <returns>The filtered query.</returns>
    public IQueryable<WorkflowInstance> Apply(IQueryable<WorkflowInstance> query)
    {
        var filter = this;
        if (!string.IsNullOrWhiteSpace(filter.Id)) query = query.Where(x => x.Id == filter.Id);
        if (filter.Ids != null) query = query.Where(x => filter.Ids.Contains(x.Id));
        if (!string.IsNullOrWhiteSpace(filter.DefinitionId)) query = query.Where(x => x.DefinitionId == filter.DefinitionId);
        if (!string.IsNullOrWhiteSpace(filter.DefinitionVersionId)) query = query.Where(x => x.DefinitionVersionId == filter.DefinitionVersionId);
        if (filter.DefinitionIds != null) query = query.Where(x => filter.DefinitionIds.Contains(x.DefinitionId));
        if (filter.DefinitionVersionIds != null) query = query.Where(x => filter.DefinitionVersionIds.Contains(x.DefinitionVersionId));
        if (filter.Version != null) query = query.Where(x => x.Version == filter.Version);
        if (!string.IsNullOrWhiteSpace(filter.CorrelationId)) query = query.Where(x => x.CorrelationId == filter.CorrelationId);
        if (filter.CorrelationIds != null) query = query.Where(x => filter.CorrelationIds.Contains(x.CorrelationId!));
        if (filter.WorkflowStatus != null) query = query.Where(x => x.Status == filter.WorkflowStatus);
        if (filter.WorkflowSubStatus != null) query = query.Where(x => x.SubStatus == filter.WorkflowSubStatus);

        var searchTerm = filter.SearchTerm;
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query =
                from instance in query
                where instance.Name!.Contains(searchTerm)
                      || instance.Id.Contains(searchTerm)
                      || instance.DefinitionId.Contains(searchTerm)
                      || instance.CorrelationId!.Contains(searchTerm)
                select instance;
        }

        return query;
    }
}