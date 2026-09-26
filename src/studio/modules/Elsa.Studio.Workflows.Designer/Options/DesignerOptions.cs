namespace Elsa.Studio.Workflows.Designer.Options;

/// <summary>
/// Represents configuration options for designer.
/// </summary>
public class DesignerOptions
{
    /// <summary>
    /// Gets or sets the designer css class.
    /// </summary>
    public string? DesignerCssClass { get; set; } = "elsa-flowchart-diagram-designer-v2";
    /// <summary>
    /// Gets or sets the graph settings.
    /// </summary>
    public X6GraphSettings GraphSettings { get; set; } = new();

    /// <summary>
    /// When true, the flowchart designer wrapper renders the React Flow-based
    /// <see cref="Components.ReactFlowDesigner"/> instead of the X6-based
    /// <see cref="Components.FlowchartDesigner"/>. Defaults to false.
    /// </summary>
    public bool UseReactFlow { get; set; }
}