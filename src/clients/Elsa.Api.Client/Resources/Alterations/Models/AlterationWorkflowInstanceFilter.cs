using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Shared.Models;

namespace Elsa.Api.Client.Resources.Alterations.Models;

/// <summary>
/// Represents a filter for workflow instances.
/// </summary>
public class AlterationWorkflowInstanceFilter
{
    /// <summary>
    /// If the filter is empty, all records are matched.
    /// </summary>
    public bool EmptyFilterSelectsAll { get; set; }
    
    /// <summary>
    /// The IDs of the workflow instances that this plan applies to.
    /// </summary>
    public IEnumerable<string>? WorkflowInstanceIds { get; set; }

    /// <summary>
    /// The correlation IDs of the workflow instances that this plan applies to.
    /// </summary>
    public IEnumerable<string>? CorrelationIds { get; set; }
    
    /// <summary>
    /// A collection of names associated with the workflow instances being filtered.
    /// </summary>
    public ICollection<string>? Names { get; set; }

    /// <summary>
    /// A search term used to filter workflow instances based on matching criteria.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// A collection of timestamp filters used for filtering data based on specified timestamp columns and operators.
    /// </summary>
    public IEnumerable<TimestampFilter>? TimestampFilters { get; set; }

    /// <summary>
    /// The IDs of the workflow definitions that this plan applies to.
    /// </summary>
    public ICollection<string>? DefinitionIds { get; set; }
    
    /// <summary>
    /// The IDs of the workflow definitions that this plan applies to.
    /// </summary>
    public IEnumerable<string>? DefinitionVersionIds { get; set; }

    /// <summary>
    /// Whether the workflow instances to match have incidents.
    /// </summary>
    public bool? HasIncidents { get; set; }

    /// <summary>
    /// Whether the workflow instances to match are system workflows. Defaults to <c>false</c>.
    /// </summary>
    public bool? IsSystem { get; set; } = false;
    
    /// <summary>
    /// Represents the workflow statuses included in the filter.
    /// </summary>
    public ICollection<WorkflowStatus>? Statuses { get; set; }

    /// <summary>
    /// A collection of sub-statuses used to filter workflow instances by their specific sub-state.
    /// </summary>
    public ICollection<WorkflowSubStatus>? SubStatuses { get; set; }
    
    /// <summary>
    /// Represents a collection of filters for activities.
    /// </summary>
    public IEnumerable<ActivityFilter>? ActivityFilters { get; set; }
    
}