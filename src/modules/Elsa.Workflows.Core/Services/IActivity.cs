using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Represents an activity, which is an atomic unit of operation within a workflow.
/// </summary>
public interface IActivity
{
    /// <summary>
    /// An identifier that is unique within the workflow. 
    /// </summary>
    string Id { get; set; }
    
    /// <summary>
    /// The logical type name of the activity.
    /// </summary>
    string Type { get; set; }
    
    /// <summary>
    /// The version of the activity type.
    /// </summary>
    int Version { get; set; }

    /// <summary>
    /// A flag indicating whether this activity can be used for starting a workflow.
    /// Usually used for triggers, but also used to disambiguate between two or more starting activities and no starting activity was specified.
    /// </summary>
    bool CanStartWorkflow { get; set; }
    
    /// <summary>
    /// A flag indicating if this activity should execute synchronously or asynchronously.
    /// By default, activities with an <see cref="ActivityKind"/> of <see cref="ActivityKind.Action"/>, <see cref="ActivityKind.Task"/> or <see cref="ActivityKind.Trigger"/>
    /// will execute synchronously, while activities of the <see cref="ActivityKind.Job"/> kind will execute asynchronously.
    /// </summary>
    bool RunAsynchronously { get; set; }
    
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
    /// The source file where this activity was instantiated, if any.
    /// </summary>
    string? Source { get; set; }

    /// <summary>
    /// The source file line number where this activity was instantiated, if any.
    /// </summary>
    int? Line { get; set; }
    
    /// <summary>
    /// Invoked when the activity executes.
    /// </summary>
    ValueTask ExecuteAsync(ActivityExecutionContext context);
}