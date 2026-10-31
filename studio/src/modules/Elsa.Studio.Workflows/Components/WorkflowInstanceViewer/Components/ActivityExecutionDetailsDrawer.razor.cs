using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// <summary>
/// Right-side drawer that shows one activity execution's State, Outcomes, Output, and Retry Attempts.
/// </summary>
public partial class ActivityExecutionDetailsDrawer
{
    /// <summary>
    /// Whether the drawer is open.
    /// </summary>
    [Parameter] public bool Open { get; set; }

    /// <summary>
    /// Raised when the drawer requests an open-state change (header close or overlay).
    /// </summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>
    /// The execution record to display.
    /// </summary>
    [Parameter] public ActivityExecutionRecord? ActivityExecution { get; set; }

    private Task Close() => OnOpenChanged(false);

    private Task OnOpenChanged(bool open) => OpenChanged.HasDelegate
        ? OpenChanged.InvokeAsync(open)
        : Task.CompletedTask;
}
