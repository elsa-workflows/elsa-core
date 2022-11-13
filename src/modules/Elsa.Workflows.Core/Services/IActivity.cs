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
    /// A value indicating whether this activity can start instances of the workflow it is a part of.
    /// </summary>
    public bool CanStartWorkflow { get; set; }
    
    /// <summary>
    /// A value indicating whether this activity can be executed asynchronously in the background.
    /// </summary>
    public bool RunAsynchronously { get; set; }
    
    /// <summary>
    /// Can contain application-specific information about this activity.
    /// </summary>
    IDictionary<string, object> ApplicationProperties { get; set; }
    
    /// <summary>
    /// Invoked when the activity executes.
    /// </summary>
    ValueTask ExecuteAsync(ActivityExecutionContext context);
}