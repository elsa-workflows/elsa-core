using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Properties.Sections.Settings;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Widgets;

/// <summary>
/// A widget that renders the workflow definition settings.
/// </summary>
public class WorkflowDefinitionSettingsWidget : WorkflowDefinitionPropertiesWidgetBase
{
    /// <inheritdoc />
    public override double Order => 20;
    
    /// <inheritdoc />
    public override Func<IDictionary<string, object?>, RenderFragment> Render => attributes =>
    {
        return builder =>
        {
            builder.OpenComponent<Settings>(0);
            builder.AddAttribute(1, nameof(Settings.WorkflowDefinition), attributes["WorkflowDefinition"]);
            builder.AddAttribute(2, nameof(Settings.WorkflowDefinitionUpdated), attributes["WorkflowDefinitionUpdated"]);
            builder.CloseComponent();
        };
    };
}