using Elsa.Studio.Contracts;
using Elsa.Studio.Diagnostics.ConsoleLogs.UI.Components;
using Elsa.Studio.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using WorkflowZoneNames = Elsa.Studio.Workflows.Constants.ZoneNames;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.UI.Widgets;

/// <summary>
/// Adds a workflow-scoped console logs tab to the workflow instance viewer left panel.
/// </summary>
public class WorkflowInstanceConsoleLogsLeftPanelTabWidget(ILocalizer localizer) : IWidget
{
    private const string ZoneName = WorkflowZoneNames.WorkflowInstanceViewerLeftPanelTabs;

    /// <inheritdoc />
    public string Zone => ZoneName;

    /// <inheritdoc />
    public double Order => 500;

    /// <inheritdoc />
    public Func<IDictionary<string, object?>, RenderFragment> Render => attributes => builder =>
    {
        var workflowInstanceId = attributes.TryGetValue("WorkflowInstanceId", out var id) ? id as string : null;

        builder.OpenComponent<MudTabPanel>(0);
        builder.AddAttribute(1, nameof(MudTabPanel.Text), localizer["Console"]);
        builder.AddAttribute(2, nameof(MudTabPanel.ChildContent), (RenderFragment)(contentBuilder =>
        {
            contentBuilder.OpenComponent<ConsoleLogViewer>(0);
            contentBuilder.AddAttribute(1, nameof(ConsoleLogViewer.ShowFilters), false);
            contentBuilder.AddAttribute(2, nameof(ConsoleLogViewer.WorkflowInstanceId), workflowInstanceId);
            contentBuilder.AddAttribute(3, nameof(ConsoleLogViewer.VisibleRowCap), 1_000);
            contentBuilder.AddAttribute(4, nameof(ConsoleLogViewer.Class), "console-log-viewer-embedded");
            contentBuilder.AddAttribute(5, nameof(ConsoleLogViewer.Style), "height: calc(100vh - var(--mud-appbar-height) - 48px); min-height: 240px;");
            contentBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    };
}
