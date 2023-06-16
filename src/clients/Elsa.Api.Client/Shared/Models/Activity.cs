namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents an activity in a workflow definition.
/// </summary>
public class Activity
{
    /// <summary>
    /// Gets or sets the ID of this activity.
    /// </summary>
    public string Id { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the type of this activity.
    /// </summary>
    public string Type { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the version of this activity.
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// Gets or sets the metadata of this activity.
    /// </summary>
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets custom properties of this activity.
    /// </summary>
    public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();

}

/// <summary>
/// Represents a flowchart activity.
/// </summary>
public class Flowchart : Container
{
    public ICollection<Connection> Type1 { get; set; }
}

/// <summary>
/// Represents a connection between two activities.
/// </summary>
public class Connection
{
    
}