using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Represents an activity, which is an atomic unit of operation within a workflow.
/// </summary>
public interface IActivity
{
    /// <summary>
    /// An identifier that is unique within a collection of activities. 
    /// </summary>
    string Id { get; set; }
    
    /// <summary>
    /// An optional name by which the activity can be referenced.
    /// </summary>
    string? Name { get; set; }
    
    /// <summary>
    /// The logical type name of the activity.
    /// </summary>
    string Type { get; set; }
    
    /// <summary>
    /// The version of the activity type.
    /// </summary>
    int Version { get; set; }

    /// <summary>
    /// A bag of properties that can be used by custom activities and other code such as middleware components to store additional values with the activity.
    /// </summary>
    IDictionary<string, object> CustomProperties { get; set; }

    /// <summary>
    /// Synthetic properties are dynamic properties not found on the activity class itself.
    /// </summary>
    [JsonIgnore]
    IDictionary<string, object> SyntheticProperties { get; set; }
    
    /// <summary>
    /// Metadata associated with the activity. This is typically used by the workflow designer to store information about the activity.
    /// </summary>
    IDictionary<string, object> Metadata { get; set; }
    
    /// <summary>
    /// Returns a value indicating whether the activity can execute.
    /// </summary>
    ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context);
    
    /// <summary>
    /// Invoked when the activity executes.
    /// </summary>
    ValueTask ExecuteAsync(ActivityExecutionContext context);
}