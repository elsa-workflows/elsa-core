using Elsa.Workflows;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Retention.Models;

/// <summary>
///     A filter for querying workflow instances.
/// </summary>
public class RetentionWorkflowInstanceFilter
{
    /// <summary>
    ///     Filter workflow instances that match the specified search term.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    ///     Filter workflow instances by definition ID.
    /// </summary>
    public string? DefinitionId { get; set; }

    /// <summary>
    ///     Filter workflow instances by definition version ID.
    /// </summary>
    public string? DefinitionVersionId { get; set; }

    /// <summary>
    ///     Filter workflow instances by definition IDs.
    /// </summary>
    public ICollection<string>? DefinitionIds { get; set; }

    /// <summary>
    ///     Filter workflow instances by correlation ID.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    ///     Filter workflow instances by correlation IDs.
    /// </summary>
    public ICollection<string>? CorrelationIds { get; set; }

    /// <summary>
    ///     Filter workflow instances by status.
    /// </summary>
    public WorkflowStatus? WorkflowStatus { get; set; }

    /// <summary>
    ///     Filter workflow instances by a set of statuses.
    /// </summary>
    public ICollection<WorkflowStatus>? WorkflowStatuses { get; set; }

    /// <summary>
    ///     Filter workflow instances by sub-status.
    /// </summary>
    public WorkflowSubStatus? WorkflowSubStatus { get; set; }

    /// <summary>
    ///     Filter workflow instances by a set of sub-status.
    /// </summary>
    public ICollection<WorkflowSubStatus>? WorkflowSubStatuses { get; set; }

    /// <summary>
    ///     Filter workflow instances by whether they have incidents.
    /// </summary>
    public bool? HasIncidents { get; set; }

    /// <summary>
    ///     Filter on workflows that are system workflows.
    /// </summary>
    public bool? IsSystem { get; set; }

    /// <summary>
    ///     Filter workflow instances by timestamp.
    /// </summary>
    public ICollection<TimestampFilter>? TimestampFilters { get; set; }

    /// <summary>
    ///     Creates a workflow instance filter based on the current filter
    /// </summary>
    /// <returns></returns>
    public WorkflowInstanceFilter Build()
    {
        return new WorkflowInstanceFilter
        {
            CorrelationId = CorrelationId,
            CorrelationIds = CorrelationIds,
            DefinitionId = DefinitionId,
            DefinitionIds = DefinitionIds,
            SearchTerm = SearchTerm,
            WorkflowStatus = WorkflowStatus,
            WorkflowStatuses = WorkflowStatuses,
            HasIncidents = HasIncidents,
            IsSystem = IsSystem,
            TimestampFilters = TimestampFilters,
            DefinitionVersionId = DefinitionVersionId,
            WorkflowSubStatuses = WorkflowSubStatuses,
            WorkflowSubStatus = WorkflowSubStatus
        };
    }
}