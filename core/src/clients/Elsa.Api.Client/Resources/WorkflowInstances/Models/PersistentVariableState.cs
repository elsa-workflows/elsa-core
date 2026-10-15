using System.Text.Json.Serialization;

namespace Elsa.Api.Client.Resources.WorkflowInstances.Models;

/// <summary>
/// Represents the state of a workflow instance's persistent variable.
/// </summary>
public class PersistentVariableState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PersistentVariableState"/> class.
    /// </summary>
    [JsonConstructor]
    public PersistentVariableState()
    {
    }
    
    /// <summary>
    /// The name of the variable.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// The ID of the storage driver.
    /// </summary>
    public string StorageDriverId { get; set; } = default!;
}