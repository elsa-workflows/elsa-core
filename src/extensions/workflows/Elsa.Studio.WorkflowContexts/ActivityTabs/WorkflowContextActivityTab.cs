using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.WorkflowContexts.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.WorkflowContexts.ActivityTabs;

/// <summary>
/// Represents an activity tab that displays the workflow context in the Elsa Studio interface.
/// </summary>
public class WorkflowContextActivityTab(ILocalizer localizer) : IActivityTab
{
    /// <inheritdoc />
    public string Title => localizer["Workflow Context"];

    /// <inheritdoc />
    public double Order => 50;

    /// <inheritdoc />
    public Func<IDictionary<string, object?>, RenderFragment> Render => attributes => builder =>
    {
        builder.OpenComponent<WorkflowContextActivityTabPanel>(0);
        builder.AddAttribute(1, nameof(WorkflowContextActivityTabPanel.WorkflowDefinition), attributes["WorkflowDefinition"]);
        builder.AddAttribute(2, nameof(WorkflowContextActivityTabPanel.Activity), attributes["Activity"]);
        builder.AddAttribute(2, nameof(WorkflowContextActivityTabPanel.ActivityDescriptor), attributes["ActivityDescriptor"]);
        builder.AddAttribute(2, nameof(WorkflowContextActivityTabPanel.OnActivityUpdated), attributes["OnActivityUpdated"]);
        builder.CloseComponent();
    };
}