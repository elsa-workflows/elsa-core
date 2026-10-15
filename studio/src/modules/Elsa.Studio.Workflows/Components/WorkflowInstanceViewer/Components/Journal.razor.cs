using Elsa.Api.Client.RealTime.Messages;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// Displays the journal for a workflow instance.
public partial class Journal : IAsyncDisposable
{
    private IList<JournalEntry> _currentEntries = null!;
    private WorkflowInstance? _workflowInstance;

    /// Gets or sets a callback invoked when a journal entry is selected.
    [Parameter] public Func<JournalEntry, Task>? JournalEntrySelected { get; set; }
    
    /// Gets or sets a function to retrieve the custom display name for an activity by its ID.
    [Parameter] public Func<string, string?>? GetCustomActivityName { get; set; }
    
    /// Gets or sets a function to retrieve the activity name for an activity by its ID.
    [Parameter] public Func<string, string?>? GetActivityName { get; set; }

    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = null!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = null!;
    [Inject] private IWorkflowInstanceObserverFactory WorkflowInstanceObserverFactory { get; set; } = null!;

    private WorkflowInstance? WorkflowInstance { get; set; }
    private IWorkflowInstanceObserver? WorkflowInstanceObserver { get; set; }
    private TimeMetricMode TimeMetricMode { get; set; } = TimeMetricMode.Relative;
    private bool ShowScopedEvents { get; set; } = true;
    private bool ShowIncidents { get; set; }
    private JournalEntry? SelectedEntry { get; set; }
    private JournalFilter? JournalFilter { get; set; }
    private Virtualize<JournalEntry> VirtualizeComponent { get; set; } = null!;
    private int SelectedIndex { get; set; } = -1;

    /// Sets the workflow instance to display the journal for.
    public async Task SetWorkflowInstanceAsync(WorkflowInstance workflowInstance, JournalFilter? filter = null)
    {
        WorkflowInstance = _workflowInstance = workflowInstance;
        JournalFilter = filter;
        await EnsureActivityDescriptorsAsync();
        await RefreshJournalAsync();
        await UpdateObserverAsync();
        StateHasChanged();
    }

    /// Clears the selection.
    public void ClearSelection()
    {
        SelectedEntry = null;
        SelectedIndex = -1;
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await EnsureActivityDescriptorsAsync();
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var hasDifferentState = _workflowInstance?.Id != WorkflowInstance?.Id || _workflowInstance?.Status != WorkflowInstance?.Status;

        if (_workflowInstance != WorkflowInstance)
        {
            _workflowInstance = WorkflowInstance;

            if (hasDifferentState) 
                await UpdateObserverAsync();
        }
    }

    private async Task EnsureActivityDescriptorsAsync() => await ActivityRegistry.EnsureLoadedAsync();

    private async Task RefreshJournalAsync()
    {
        if (VirtualizeComponent != null!)
            await VirtualizeComponent.RefreshDataAsync();
    }
    
    private async Task UpdateObserverAsync()
    {
        if (WorkflowInstance?.Status == WorkflowStatus.Running)
            await ObserveWorkflowInstanceAsync();
    }

    private TimeSpan GetTimeMetric(WorkflowExecutionLogRecord current, WorkflowExecutionLogRecord? previous)
    {
        return TimeMetricMode switch
        {
            TimeMetricMode.Relative => previous == null ? TimeSpan.Zero : current.Timestamp - previous.Timestamp,
            TimeMetricMode.Accumulated => SumExecutionTime(current),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private TimeSpan SumExecutionTime(WorkflowExecutionLogRecord current) => current.Timestamp - WorkflowInstance!.CreatedAt;

    private async ValueTask<ItemsProviderResult<JournalEntry>> FetchExecutionLogRecordsAsync(ItemsProviderRequest request)
    {
        if (WorkflowInstance == null)
            return new([], 0);

        await EnsureActivityDescriptorsAsync();

        var take = request.Count == 0 ? 10 : request.Count;
        var skip = request.StartIndex > 0 ? request.StartIndex - 1 : 0;
        var filter = new JournalFilter();

        if (ShowScopedEvents) filter.ActivityNodeIds = JournalFilter?.ActivityNodeIds;
        if (ShowIncidents) filter.EventNames = ["Faulted"];

        filter.ExcludedActivityTypes = ["Elsa.Workflow", "Elsa.Flowchart"];

        var response = await WorkflowInstanceService.GetJournalAsync(WorkflowInstance.Id, filter, skip, take);
        var totalCount = request.StartIndex > 0 ? response.TotalCount - 1 : response.TotalCount;
        var records = response.Items.ToArray();
        var localSkip = request.StartIndex > 0 ? 1 : 0;
        var entries = records.Skip(localSkip).Select((record, index) =>
        {
            var previousIndex = index - 1;
            var previousRecord = previousIndex >= 0 ? records[previousIndex] : null;
            var activityDescriptor = ActivityRegistry.Find(record.ActivityType, record.ActivityTypeVersion);
            var activityDisplaySettings = ActivityDisplaySettingsRegistry.GetSettings(record.ActivityType);
            var isEven = index % 2 == 0;
            var timeMetric = GetTimeMetric(record, previousRecord);

            return new JournalEntry(
                record,
                activityDescriptor,
                activityDisplaySettings,
                isEven,
                timeMetric);
        }).ToList();

        var selectedEntry = SelectedEntry;
        _currentEntries = entries;

        // If the selected entry is still in the list, select it again.
        SelectedEntry = entries.FirstOrDefault(x => x.Record.Id == selectedEntry?.Record.Id);

        return new(entries, (int)totalCount);
    }

    private async Task DisposeObserverAsync()
    {
        if (WorkflowInstanceObserver != null)
        {
            WorkflowInstanceObserver.WorkflowJournalUpdated -= OnWorkflowJournalUpdatedAsync;
            await WorkflowInstanceObserver.DisposeAsync();
            WorkflowInstanceObserver = null;
        }
    }

    private async Task ObserveWorkflowInstanceAsync()
    {
        await DisposeObserverAsync();
        WorkflowInstanceObserver = await WorkflowInstanceObserverFactory.CreateAsync(WorkflowInstance!.Id);
        WorkflowInstanceObserver.WorkflowJournalUpdated += OnWorkflowJournalUpdatedAsync;
    }

    private async Task OnWorkflowJournalUpdatedAsync(WorkflowExecutionLogUpdatedMessage message)
    {
        await InvokeAsync(RefreshJournalAsync);
    }

    private async Task OnTimeMetricButtonToggleChanged(bool value)
    {
        TimeMetricMode = value ? TimeMetricMode.Accumulated : TimeMetricMode.Relative;
        await RefreshJournalAsync();
    }

    private async Task OnScopeToggleChanged(bool value)
    {
        ShowScopedEvents = value;
        await RefreshJournalAsync();
    }

    private async Task OnShowIncidentsToggleChanged(bool value)
    {
        ShowIncidents = value;
        await RefreshJournalAsync();
    }

    private async Task OnJournalEntrySelected(int index)
    {
        if (index < 0 || index >= _currentEntries.Count)
        {
            SelectedEntry = null;
            SelectedIndex = -1;
        }

        var entry = index >= 0 && index < _currentEntries.Count ? _currentEntries[index] : null;
        SelectedEntry = entry;
        SelectedIndex = index;

        if (JournalEntrySelected != null && entry != null)
            await JournalEntrySelected(entry);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeObserverAsync();
    }
}