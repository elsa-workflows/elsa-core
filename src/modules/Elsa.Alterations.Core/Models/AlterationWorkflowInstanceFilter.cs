using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;

namespace Elsa.Alterations.Core.Models;

/// <summary>
/// Represents a filter for workflow instances.
/// </summary>
[UsedImplicitly]
public class AlterationWorkflowInstanceFilter
{
    /// <summary>
    /// The IDs of the workflow instances that this plan applies to.
    /// </summary>
    public IEnumerable<string>? WorkflowInstanceIds { get; set; }

    /// <summary>
    /// The correlation IDs of the workflow instances that this plan applies to.
    /// </summary>
    public IEnumerable<string>? CorrelationIds { get; set; }

    /// <summary>
    /// A collection of timestamp filters used for filtering data based on specified timestamp columns and operators.
    /// </summary>
    public IEnumerable<TimestampFilter>? TimestampFilters { get; set; }
    
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
    /// Represents a collection of filters for activities.
    /// </summary>
    public IEnumerable<ActivityFilter>? ActivityFilters { get; set; }
    
}