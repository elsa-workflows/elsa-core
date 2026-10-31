using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Api.Client.Resources.WorkflowInstances.Models;

/// <summary>
/// A serializable shape of an <c>ActivityExecutionContext</c>.
/// </summary>
public class ActivityExecutionContextState
{
    /// <summary>
    /// The ID of the activity instance.
    /// </summary>
    public string Id { get; set; } = default!;
    
    /// <summary>
    /// The ID of the parent of the activity instance.
    /// </summary>
    public string? ParentContextId { get; set; }
    
    /// <summary>
    /// The node ID of the scheduled activity.
    /// </summary>
    public string ScheduledActivityNodeId { get; set; } = default!;
    
    /// <summary>
    /// The node ID of the activity that owns the scheduled activity.
    /// </summary>
    public string? OwnerActivityNodeId { get; set; }
    
    /// <summary>
    /// A bag of properties.
    /// </summary>
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// The evaluated values of the activity's properties.
    /// </summary>
    public IDictionary<string, object>? ActivityState { get; set; }

    /// <summary>
    /// A list of dynamically created variables.
    /// </summary>
    public ICollection<Variable> DynamicVariables { get; set; } = new List<Variable>();
}