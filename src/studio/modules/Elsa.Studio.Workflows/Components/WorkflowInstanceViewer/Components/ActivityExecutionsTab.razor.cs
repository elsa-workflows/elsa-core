using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// Displays the list of activity executions.
public partial class ActivityExecutionsTab
{
    /// Represents a row in the table of activity executions.
    /// <param name="Number">The number of executions.</param>
    /// <param name="ActivityExecutionSummary">The activity execution summary.</param>
    public record ActivityExecutionRecordTableRow(int Number, ActivityExecutionRecordSummary ActivityExecutionSummary)
    {
        /// Indicates whether the activity execution record has recorded retries.
        public bool HasRetries => ActivityExecutionSummary.Metadata != null && ActivityExecutionSummary.Metadata.TryGetValue("HasRetryAttempts", out var retryAttempts) && retryAttempts is JsonElement { ValueKind: JsonValueKind.True };
    }

    /// The height of the visible pane.
    [Parameter] public int VisiblePaneHeight { get; set; }

    /// The activity to display executions for.
    [Parameter] public JsonObject Activity { get; set; } = null!;

    /// The activity execution record summaries.
    [Parameter] public ICollection<ActivityExecutionRecordSummary> ActivityExecutionSummaries { get; set; } = new List<ActivityExecutionRecordSummary>();

    /// A callback invoked when an activity execution is selected.
    [Parameter] public EventCallback<string> ExecutionSelected { get; set; }

    private IEnumerable<ActivityExecutionRecordTableRow> Items => ActivityExecutionSummaries.Select((x, i) => new ActivityExecutionRecordTableRow(i + 1, x));

    private async Task OnActivityExecutionClicked(TableRowClickEventArgs<ActivityExecutionRecordTableRow> arg)
    {
        var id = arg.Item?.ActivityExecutionSummary.Id;

        if (id == null)
            return;

        if (ExecutionSelected.HasDelegate)
            await ExecutionSelected.InvokeAsync(id);
    }
}
