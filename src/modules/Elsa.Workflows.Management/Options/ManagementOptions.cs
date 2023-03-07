using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Options;

/// <summary>
/// Options for the management module.
/// </summary>
public class ManagementOptions
{
    /// <summary>
    /// A collection of activity types that are available to the system.
    /// </summary>
    public HashSet<Type> ActivityTypes { get; set; } = new();
    
    /// <summary>
    /// A collection of types that are available to the system as variable types.
    /// </summary>
    public HashSet<VariableDescriptor> VariableDescriptors { get; set; } = new();
}