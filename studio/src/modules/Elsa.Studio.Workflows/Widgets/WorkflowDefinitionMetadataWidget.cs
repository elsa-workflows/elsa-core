using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Properties.Sections.Metadata;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Widgets;

/// <summary>
/// A widget that renders the workflow definition metadata.
/// </summary>
public class WorkflowDefinitionMetadataWidget : WorkflowDefinitionPropertiesWidgetBase
{
    /// <inheritdoc />
    public override double Order => 10;
    
    /// <inheritdoc />
    public override Func<IDictionary<string, object?>, RenderFragment> Render => attributes =>
    {
        return builder =>
        {
            builder.OpenComponent<Metadata>(0);
            builder.AddAttribute(1, nameof(Metadata.WorkflowDefinition), attributes["WorkflowDefinition"]);
            builder.AddAttribute(2, nameof(Metadata.WorkflowDefinitionUpdated), attributes["WorkflowDefinitionUpdated"]);
            builder.CloseComponent();
        };
    };
}