using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Diagnostics.ConsoleLogs.UI.Components;
using Elsa.Studio.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using WorkflowZoneNames = Elsa.Studio.Workflows.Constants.ZoneNames;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.UI.Widgets;

/// <summary>
/// Adds an activity-scoped console logs tab to the workflow instance viewer bottom panel.
/// </summary>
public class WorkflowInstanceConsoleLogsTabWidget : IWidget
{
    private const string ZoneName = WorkflowZoneNames.WorkflowInstanceViewerBottomTabs;
    private readonly ILocalizer _localizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowInstanceConsoleLogsTabWidget"/> class.
    /// </summary>
    public WorkflowInstanceConsoleLogsTabWidget(ILocalizer localizer)
    {
        _localizer = localizer;
    }

    /// <inheritdoc />
    public string Zone => ZoneName;

    /// <inheritdoc />
    public double Order => 100;

    /// <inheritdoc />
    public Func<IDictionary<string, object?>, RenderFragment> Render => attributes => builder =>
    {
        var workflowInstanceId = attributes.TryGetValue("WorkflowInstanceId", out var id) ? id as string : null;
        var selectedActivity = attributes.TryGetValue("SelectedActivity", out var activityValue) ? activityValue as JsonObject : null;
        var lastActivityExecution = attributes.TryGetValue("LastActivityExecution", out var executionValue) ? executionValue as ActivityExecutionRecord : null;
        var visiblePaneHeight = attributes.TryGetValue("VisiblePaneHeight", out var height) && height is int value ? value : (int?)null;
        var style = visiblePaneHeight is > 0 ? $"height: {Math.Max(visiblePaneHeight.Value - 48, 120)}px;" : null;
        var activityId = selectedActivity?.GetId();
        var activityNodeId = selectedActivity?.GetNodeId() ?? lastActivityExecution?.ActivityNodeId;
        var activityInstanceId = lastActivityExecution?.Id;

        builder.OpenComponent<MudTabPanel>(0);
        builder.AddAttribute(1, nameof(MudTabPanel.Text), _localizer["Console"]);
        builder.AddAttribute(2, nameof(MudTabPanel.ChildContent), (RenderFragment)(contentBuilder =>
        {
            contentBuilder.OpenComponent<ConsoleLogViewer>(0);
            contentBuilder.AddAttribute(1, nameof(ConsoleLogViewer.ShowFilters), false);
            contentBuilder.AddAttribute(2, nameof(ConsoleLogViewer.WorkflowInstanceId), workflowInstanceId);
            contentBuilder.AddAttribute(3, nameof(ConsoleLogViewer.ActivityInstanceId), activityInstanceId);
            contentBuilder.AddAttribute(4, nameof(ConsoleLogViewer.ActivityId), activityId);
            contentBuilder.AddAttribute(5, nameof(ConsoleLogViewer.ActivityNodeId), activityNodeId);
            contentBuilder.AddAttribute(6, nameof(ConsoleLogViewer.Class), "console-log-viewer-embedded");
            contentBuilder.AddAttribute(7, nameof(ConsoleLogViewer.Style), style);
            contentBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    };
}
