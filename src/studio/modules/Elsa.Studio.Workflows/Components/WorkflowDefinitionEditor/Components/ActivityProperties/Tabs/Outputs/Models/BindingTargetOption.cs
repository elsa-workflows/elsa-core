namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs.Outputs.Models;

/// <summary>
/// Represents the binding target option record.
/// </summary>
public record BindingTargetOption(string Text, string Value, string TypeName, bool IsArray)
{
    /// <summary>
    /// Gets the declared type name, including the array suffix when applicable.
    /// </summary>
    public string DeclaredTypeName => IsArray ? $"{TypeName}[]" : TypeName;
}
