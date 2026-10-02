using Elsa.Studio.Contracts;
using Elsa.Studio.WorkflowContexts.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.WorkflowContexts.Widgets;

/// <summary>
/// A widget that renders the workflow context editor.
/// </summary>
public class WorkflowContextsEditorWidget : IWidget
{
    /// <inheritdoc />
    public string Zone => "workflow-definition-properties";

    /// <inheritdoc />
    public double Order => 25;

    /// <inheritdoc />
    public Func<IDictionary<string, object?>, RenderFragment> Render => attributes => builder =>
    {
        builder.OpenComponent<WorkflowContextsEditor>(0);
        builder.AddAttribute(1, nameof(WorkflowContextsEditor.WorkflowDefinition), attributes["WorkflowDefinition"]);
        builder.AddAttribute(2, nameof(WorkflowContextsEditor.WorkflowDefinitionUpdated), attributes["WorkflowDefinitionUpdated"]);
        builder.CloseComponent();

    };
}