namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Variables.Models;

/// <summary>
/// Represents a variable test value model.
/// </summary>
public class VariableTestValueModel
{
    /// <summary>
    /// Gets or sets the variable id.
    /// </summary>
    public string VariableId { get; set; } = null!;
    /// <summary>
    /// Gets or sets the test value.
    /// </summary>
    public string? TestValue { get; set; }
}