using Elsa.Workflows.LogPersistence;
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

    /// <summary>
    /// The format to use for compressing workflow state.
    /// </summary>
    public string? CompressionAlgorithm { get; set; }

    /// <summary>
    /// The default Log Persistence Mode to use for all the system
    /// </summary>
    public LogPersistenceMode LogPersistenceMode { get; set; }

    /// <summary>
    /// A mode that does not allow editing workflows.
    /// </summary>
    public bool IsReadOnlyMode { get; set; }
}