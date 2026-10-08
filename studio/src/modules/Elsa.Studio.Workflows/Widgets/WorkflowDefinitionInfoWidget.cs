using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Properties.Sections.Info;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Widgets;

/// <summary>
/// A widget that renders the workflow definition info.
/// </summary>
public class WorkflowDefinitionInfoWidget : WorkflowDefinitionPropertiesWidgetBase
{
    /// <inheritdoc />
    public override double Order => 30;

    /// <inheritdoc />
    public override Func<IDictionary<string, object?>, RenderFragment> Render => attributes =>
    {
        return builder =>
        {
            builder.OpenComponent<Info>(0);
            builder.AddAttribute(1, nameof(Info.WorkflowDefinition), attributes["WorkflowDefinition"]);
            builder.CloseComponent();
        };
    };
}