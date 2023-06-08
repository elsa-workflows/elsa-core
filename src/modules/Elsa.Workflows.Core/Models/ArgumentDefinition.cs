namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Base class for workflow input and output definitions.
/// </summary>
public abstract class ArgumentDefinition
{
    /// <summary>
    /// The type of the input value.
    /// </summary>
    public Type Type { get; set; } = default!;

    /// <summary>
    /// The technical name of the input.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// A user friendly name of the input.
    /// </summary>
    public string DisplayName { get; set; } = default!;

    /// <summary>
    /// A description of the input.
    /// </summary>
    public string Description { get; set; } = default!;

    /// <summary>
    /// The category to which this input belongs.
    /// </summary>
    public string Category { get; set; } = default!;
}