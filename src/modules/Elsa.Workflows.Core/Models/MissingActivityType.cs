namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Represents a missing activity type.
/// </summary>
public class MissingActivityType
{
    /// <summary>
    /// The type name of the missing activity type.
    /// </summary>
    public string TypeName { get; set; } = default!;
    
    /// <summary>
    /// The version of the missing activity type.
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// The original activity JSON.
    /// </summary>
    public string ActivityJson { get; set; } = default!;
    
    /// <summary>
    /// The original activity outcomes.
    /// </summary>
    public ICollection<string> Outcomes { get; set; } = new List<string>();
}