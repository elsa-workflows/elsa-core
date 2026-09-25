using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.VariableTypes.Models;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Variables.Models;

/// <summary>
/// Represents a variable model.
/// </summary>
public class VariableModel
{
    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    public string Id { get; set; } = null!;
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = null!;
    /// <summary>
    /// Gets or sets the variable type.
    /// </summary>
    public VariableTypeDescriptor VariableType { get; set; } = null!;
    /// <summary>
    /// Indicates whether is array.
    /// </summary>
    public bool IsArray { get; set; }
    /// <summary>
    /// Gets or sets the default value.
    /// </summary>
    public string? DefaultValue { get; set; }
    /// <summary>
    /// Gets or sets the storage driver.
    /// </summary>
    public StorageDriverDescriptor StorageDriver { get; set; } = null!;
}