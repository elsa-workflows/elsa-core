namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.Models;

/// <summary>
/// Provides configuration options for the workflow definition editor.
/// </summary>
public class WorkflowDefinitionOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether changes are automatically saved.
    /// </summary>
    public bool AutoSaveChangesByDefault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether changes made in the Monaco editor are automatically applied to the workflow definition.
    /// </summary>
    public bool AutoApplyCodeViewChangesByDefault { get; set; }
}