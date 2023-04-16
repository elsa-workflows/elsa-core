using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.State;

/// <summary>
/// A serializable shape of <see cref="ActivityExecutionContext"/>.
/// </summary>
public class ActivityExecutionContextState
{
    // ReSharper disable once EmptyConstructor
    // Required for JSON serialization configured with reference handling.
    /// <summary>
    /// Constructor.
    /// </summary>
    public ActivityExecutionContextState()
    {
    }

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
    
    //public RegisterState Register { get; set; } = default!;
}