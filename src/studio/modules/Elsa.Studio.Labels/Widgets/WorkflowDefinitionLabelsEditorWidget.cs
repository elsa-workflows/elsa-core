using Elsa.Studio.Contracts;
using Elsa.Studio.Labels.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.WorkflowContexts.Widgets;

/// <summary>
/// A widget that renders the workflow context editor.
/// </summary>
public class WorkflowDefinitionLabelsEditorWidget : IWidget
{
    /// <inheritdoc />
    public string Zone => "workflow-definition-properties";

    /// <inheritdoc />
    public double Order => 25;

    /// <inheritdoc />
    public Func<IDictionary<string, object?>, RenderFragment> Render => attributes => builder =>
    {
        builder.OpenComponent<WorkflowDefinitionLabelsEditor>(0);
        builder.AddAttribute(1, nameof(WorkflowDefinitionLabelsEditor.WorkflowDefinition), attributes["WorkflowDefinition"]);
        builder.AddAttribute(2, nameof(WorkflowDefinitionLabelsEditor.WorkflowDefinitionUpdated), attributes["WorkflowDefinitionUpdated"]);
        builder.CloseComponent();

    };
}