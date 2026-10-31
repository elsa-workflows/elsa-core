namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// Base class for workflow input and output definitions.
/// </summary>
public abstract class ArgumentDefinition
{
    /// <summary>
    /// The type of the input value.
    /// </summary>
    public string Type { get; set; } = default!;

    /// <summary>
    /// Indicates whether the input is an array.
    /// </summary>
    public bool IsArray { get; set; }

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
    
    /// <summary>
    /// The type name of the variable, including the array indicator.
    /// </summary>
    public string GetTypeDisplayName() => IsArray ? $"{Type}[]" : Type;
}