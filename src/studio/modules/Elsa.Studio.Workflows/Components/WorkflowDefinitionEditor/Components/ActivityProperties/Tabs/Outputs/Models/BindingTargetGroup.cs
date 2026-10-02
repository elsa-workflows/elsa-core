namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs.Outputs.Models;

/// <summary>
/// Represents the binding target group record.
/// </summary>
public record BindingTargetGroup(string Text, BindingKind Kind, ICollection<BindingTargetOption> Options);