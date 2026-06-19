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
    public HashSet<VariableDescriptor> VariableDescriptors { get; set; } = new(new VariableDescriptor.VariableDescriptorComparer());

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

    /// <summary>
    /// Determines whether publishing a workflow definition fails when the workflow has validation errors.
    /// When <c>false</c> (the default in 3.6.x and 3.7.x), publishing is allowed to succeed and any validation
    /// errors are returned as warnings on the publish result. When <c>true</c>, publishing fails if any validation
    /// errors are present. This restores the ability to publish workflows that intentionally leave required
    /// properties blank (for example, an empty Cron expression used to disable a trigger). As of 3.8.0 this
    /// defaults to <c>true</c> (opt-out) instead of being opt-in.
    /// </summary>
    public bool FailOnValidationErrors { get; set; }
}