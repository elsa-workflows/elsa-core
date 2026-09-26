using Elsa.Api.Client.Resources.VariableTypes.Models;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Models;

/// <summary>
/// Represents a argument definition model.
/// </summary>
public abstract class ArgumentDefinitionModel
{
    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public VariableTypeDescriptor Type { get; set; } = default!;
    /// <summary>
    /// Indicates whether is array.
    /// </summary>
    public bool IsArray { get; set; }
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = default!;
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName { get; set; } = default!;
    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; } = default!;
    /// <summary>
    /// Gets or sets the category.
    /// </summary>
    public string Category { get; set; } = default!;
}