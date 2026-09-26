using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// <summary>
/// Displays the call stack for a selected activity execution.
/// </summary>
public partial class ActivityCallStack
{
    private readonly List<ActivityCallStackEntry> _entries = new();
    private bool _isLoading;
    private string? _lastKey;
    private bool _hasError;
    private int _selectedIndex = -1;
    private ActivityCallStackEntry? _selectedEntry;

    /// <summary>
    /// The workflow instance ID associated with the selected activity.
    /// </summary>
    [Parameter] public string? WorkflowInstanceId { get; set; }

    /// <summary>
    /// The selected activity execution record ID to display the call stack for.
    /// </summary>
    [Parameter] public string? ActivityExecutionRecordId { get; set; }

    /// <summary>
    /// The selected activity node ID (used for bidirectional highlighting).
    /// </summary>
    [Parameter] public string? SelectedActivityNodeId { get; set; }

    /// <summary>
    /// Indicates whether the call stack tab is currently active.
    /// </summary>
    [Parameter] public bool IsActive { get; set; }

    /// <summary>
    /// Gets a callback invoked when a call stack entry is selected.
    /// </summary>
    [Parameter] public EventCallback<ActivityExecutionRecord> CallStackEntrySelected { get; set; }

    /// <summary>
    /// Gets or sets a function to retrieve the custom display name for an activity by its ID.
    /// </summary>
    [Parameter] public Func<string, string?>? GetCustomActivityName { get; set; }
    
    /// <summary>
    /// Gets or sets a function to retrieve the activity name for an activity by its ID.
    /// </summary>
    [Parameter] public Func<string, string?>? GetActivityName { get; set; }

    [Inject] private IActivityExecutionService ActivityExecutionService { get; set; } = null!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = null!;

    private IReadOnlyList<ActivityCallStackEntry> Entries => _entries;
    private bool IsLoading => _isLoading;
    private bool HasError => _hasError;
    private int SelectedIndex => _selectedIndex;
    private ActivityCallStackEntry? SelectedEntry => _selectedEntry;
    
    /// <summary>
    /// Provides indexed entries for virtualization.
    /// </summary>
    private List<IndexedCallStackEntry> IndexedEntries => 
        _entries.Select((entry, index) => new IndexedCallStackEntry(index, entry)).ToList();

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var key = $"{WorkflowInstanceId}|{ActivityExecutionRecordId}|{IsActive}";

        if (key == _lastKey)
        {
            if (IsActive && _entries.Any() && !string.IsNullOrWhiteSpace(SelectedActivityNodeId))
                TrySelectActivityInCurrentStack(SelectedActivityNodeId);
            return;
        }

        _lastKey = key;

        if (!IsActive || string.IsNullOrWhiteSpace(ActivityExecutionRecordId) || string.IsNullOrWhiteSpace(WorkflowInstanceId))
        {
            _entries.Clear();
            _hasError = false;
            _selectedIndex = -1;
            _selectedEntry = null;
            return;
        }

        await LoadCallStackAsync(ActivityExecutionRecordId);
    }

    private async Task LoadCallStackAsync(string activityExecutionRecordId)
    {
        _isLoading = true;
        _hasError = false;
        _entries.Clear();
        _selectedIndex = -1;
        _selectedEntry = null;
        StateHasChanged();

        try
        {
            await ActivityRegistry.EnsureLoadedAsync();

            var callStack = await ActivityExecutionService.GetCallStackAsync(activityExecutionRecordId, includeCrossWorkflowChain: true);

            foreach (var record in callStack.Items)
            {
                var activityDescriptor = ActivityRegistry.Find(record.ActivityType, record.ActivityTypeVersion);
                var activityDisplaySettings = ActivityDisplaySettingsRegistry.GetSettings(record.ActivityType);
                var duration = GetDuration(record);

                _entries.Add(new ActivityCallStackEntry(record, activityDescriptor, activityDisplaySettings, duration));
            }

            _selectedIndex = _entries.Count - 1;
            _selectedEntry = _entries.LastOrDefault();
        }
        catch
        {
            _hasError = true;
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private static TimeSpan? GetDuration(ActivityExecutionRecord record)
    {
        if (record.StartedAt == default)
            return null;

        var end = record.CompletedAt ?? DateTimeOffset.UtcNow;
        return end - record.StartedAt;
    }

    private async Task OnCallStackEntrySelected(int index)
    {
        if (index < 0 || index >= _entries.Count)
        {
            _selectedIndex = -1;
            _selectedEntry = null;
            return;
        }

        var entry = _entries[index];
        _selectedIndex = index;
        _selectedEntry = entry;

        if (CallStackEntrySelected.HasDelegate)
            await CallStackEntrySelected.InvokeAsync(entry.Record);
    }

    /// <summary>
    /// Attempts to find and select an activity in the current call stack by its node ID.
    /// This is used when the call stack is pinned to provide bidirectional highlighting.
    /// </summary>
    private void TrySelectActivityInCurrentStack(string activityNodeId)
    {
        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Record.ActivityNodeId == activityNodeId)
            {
                _selectedIndex = i;
                _selectedEntry = _entries[i];
                StateHasChanged();
                return;
            }
        }

        // Activity not found in current call stack - don't change selection
    }
    
    /// <summary>
    /// Helper record to provide index along with entry for virtualization.
    /// </summary>
    private record IndexedCallStackEntry(int Index, ActivityCallStackEntry Entry);
}
