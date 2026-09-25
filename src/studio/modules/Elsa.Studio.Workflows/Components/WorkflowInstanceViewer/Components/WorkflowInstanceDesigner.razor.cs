using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.RealTime.Messages;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Constants;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Constants;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Elsa.Studio.Workflows.Shared.Args;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Radzen;
using Radzen.Blazor;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// <summary>
/// Displays the workflow instance.
/// </summary>
public partial class WorkflowInstanceDesigner : IAsyncDisposable
{
    private WorkflowInstance? _workflowInstance = null!;
    private RadzenSplitterPane _activityPropertiesPane = null!;
    private DiagramDesignerWrapper? _designer;
    private ActivityDetailsTab? _activityDetailsTab = null!;
    private ActivityExecutionsTab? _activityExecutionsTab = null!;
    private int _propertiesPaneHeight = 300;
    private readonly Dictionary<string, ICollection<ActivityExecutionRecordSummary>> _activityExecutionRecordsLookup = new();
    private readonly Dictionary<string, ActivityExecutionRecord> _lastActivityExecutionRecordLookup = new();
    private Timer? _elapsedTimer;
    private volatile bool _disposed;
    private bool IsAlterationsEnabled { get; set; }

    /// The workflow instance.
    [Parameter] public WorkflowInstance? WorkflowInstance { get; set; }

    /// The workflow definition.
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// The path changed callback.
    [Parameter] public EventCallback<DesignerPathChangedArgs> PathChanged { get; set; }

    /// The activity selected callback.
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }

    /// An event that is invoked when the workflow definition is requested to be edited.
    [Parameter] public EventCallback<string> EditWorkflowDefinition { get; set; }

    /// An event that is invoked when an activity execution is selected.
    [Parameter] public EventCallback<string?> ActivityExecutionSelected { get; set; }

    /// Gets or sets the current selected sub-workflow.
    [Parameter] public JsonObject? SelectedSubWorkflow { get; set; }

    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IDiagramDesignerService DiagramDesignerService { get; set; } = null!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = null!;
    [Inject] private IActivityVisitor ActivityVisitor { get; set; } = null!;
    [Inject] private IActivityExecutionService ActivityExecutionService { get; set; } = null!;
    [Inject] private IWorkflowInstanceObserverFactory WorkflowInstanceObserverFactory { get; set; } = null!;
    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = null!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IRemoteFeatureProvider RemoteFeatureProvider { get; set; } = null!;

    private JsonObject? RootActivity => WorkflowDefinition?.Root;
    private JsonObject? SelectedActivity { get; set; }
    private ActivityDescriptor? ActivityDescriptor { get; set; }
    private JournalEntry? SelectedWorkflowExecutionLogRecord { get; set; }
    private IWorkflowInstanceObserver? _workflowInstanceObserver;

    private IWorkflowInstanceObserver? WorkflowInstanceObserver
    {
        get => _workflowInstanceObserver;
        set => _workflowInstanceObserver = value;
    }
    private ICollection<ActivityExecutionRecordSummary> SelectedActivityExecutions { get; set; } = new List<ActivityExecutionRecordSummary>();
    private ActivityExecutionRecord? LastActivityExecution { get; set; }
    private Timer? _refreshTimer;
    private IDictionary<string, object?> BottomPanelTabAttributes => new Dictionary<string, object?>
    {
        ["WorkflowInstanceId"] = WorkflowInstance?.Id,
        ["WorkflowDefinition"] = WorkflowDefinition,
        ["WorkflowInstance"] = WorkflowInstance,
        ["SelectedActivity"] = SelectedActivity,
        ["LastActivityExecution"] = LastActivityExecution,
        ["VisiblePaneHeight"] = _propertiesPaneHeight
    };

    private RadzenSplitterPane ActivityPropertiesPane
    {
        get => _activityPropertiesPane;
        set
        {
            _activityPropertiesPane = value;

            // Prefix the ID with a non-numerical value so it can always be used as a query selector (sometimes, Radzen generates a unique ID starting with a number).
            _activityPropertiesPane.UniqueID = $"pane-{value.UniqueID}";
        }
    }

    private MudTabs PropertyTabs { get; set; } = null!;
    private MudTabPanel EventsTabPanel { get; set; } = null!;

    /// Updates the selected sub-workflow.
    public void UpdateSubWorkflow(JsonObject? obj)
    {
        SelectedSubWorkflow = obj;
        StateHasChanged();
    }

    /// Selects the activity by its node ID.
    public async Task SelectActivityAsync(string nodeId)
    {
        if (_designer == null) return;
        await _designer.SelectActivityAsync(nodeId);
    }
    
    /// Selects the activity by its ID.
    public async Task SelectActivityByIdAsync(string activityId)
    {
        if (_designer == null) return;
        await _designer.SelectActivityByActivityIdAsync(activityId);
    }

    /// <summary>
    /// Selects the activity with the specified ID, and if not found in the current container, uses the node ID to navigate to the correct container first.
    /// </summary>
    /// <param name="activityId">The ID of the activity to select.</param>
    /// <param name="nodeId">The node ID used to navigate to the correct container when the activity is not found in the current container.</param>
    public async Task SelectActivityByIdAsync(string activityId, string nodeId)
    {
        if (_designer == null) return;
        await _designer.SelectActivityByActivityIdAsync(activityId, nodeId);
    }

    /// Sets the selected journal entry.
    public async Task SelectWorkflowExecutionLogRecordAsync(JournalEntry entry)
    {
        var activityId = entry.Record.ActivityId;
        var nodeId = entry.Record.NodeId;
        SelectedWorkflowExecutionLogRecord = entry;
        await SelectActivityByIdAsync(activityId, nodeId);
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        IsAlterationsEnabled = await RemoteFeatureProvider.IsEnabledOrDefaultAsync(RemoteFeatureNames.Alterations);
        await ActivityRegistry.EnsureLoadedAsync();

        if (WorkflowDefinition?.Root == null!)
            return;

        await UpdateObserverAsync();
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var hasDifferentState = _workflowInstance?.Id != WorkflowInstance?.Id || _workflowInstance?.Status != WorkflowInstance?.Status;

        if (_workflowInstance != WorkflowInstance)
        {
            _workflowInstance = WorkflowInstance!;

            if (hasDifferentState)
                await UpdateObserverAsync();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (WorkflowDefinition != null)
                await HandleActivitySelectedAsync(WorkflowDefinition!.Root);
            await UpdatePropertiesPaneHeightAsync();
            await UpdateObserverAsync();
        }
    }

    private async Task UpdateObserverAsync()
    {
        if (WorkflowInstance?.Status == WorkflowStatus.Running)
        {
            await CreateObserverAsync();
            StartElapsedTimer();
        }
        else
        {
            StopElapsedTimer();
        }
    }

    /// <summary>
    /// Creates and publishes a new <see cref="WorkflowInstanceObserver"/>, unless the component is or
    /// becomes disposed while the factory call is in flight. Internal so tests can invoke it directly
    /// to pin the disposal race it guards against.
    /// </summary>
    internal async Task CreateObserverAsync()
    {
        if (_workflowInstance == null || _designer == null)
            return;

        await DisposeObserverAsync();

        if (_disposed) return;

        var container = _designer.GetCurrentContainerActivityOrRoot();
        var observerContext = new WorkflowInstanceObserverContext
        {
            WorkflowInstanceId = _workflowInstance.Id,
            ContainerActivity = container,
        };
        var observer = await WorkflowInstanceObserverFactory.CreateAsync(observerContext);

        if (_disposed)
        {
            // DisposeAsync ran while the factory call above was in flight; dispose the observer we just
            // created instead of publishing and subscribing it on a torn-down component.
            await observer.DisposeAsync();
            return;
        }

        observer.ActivityExecutionLogUpdated += OnActivityExecutionLogUpdated;

        var previousObserver = Interlocked.Exchange(ref _workflowInstanceObserver, observer);

        if (previousObserver != null)
        {
            previousObserver.ActivityExecutionLogUpdated -= OnActivityExecutionLogUpdated;
            await previousObserver.DisposeAsync();
        }

        if (_disposed)
        {
            // DisposeAsync ran between the guard above and publishing the observer; detach and dispose
            // it, guarding against DisposeObserverAsync having already detached it.
            var disposedObserver = Interlocked.Exchange(ref _workflowInstanceObserver, null);

            if (disposedObserver != null)
            {
                disposedObserver.ActivityExecutionLogUpdated -= OnActivityExecutionLogUpdated;
                await disposedObserver.DisposeAsync();
            }
        }
    }

    private async Task DisposeObserverAsync()
    {
        var observer = Interlocked.Exchange(ref _workflowInstanceObserver, null);

        if (observer != null)
        {
            observer.ActivityExecutionLogUpdated -= OnActivityExecutionLogUpdated;
            await observer.DisposeAsync();
        }
    }

    private async Task OnActivityExecutionLogUpdated(ActivityExecutionLogUpdatedMessage message)
    {
        if (_designer == null) return;

        foreach (var stats in message.Stats)
        {
            var activityNodeId = stats.ActivityNodeId;
            var activityId = stats.ActivityId;
            _activityExecutionRecordsLookup.Remove(activityNodeId);
            _lastActivityExecutionRecordLookup.Remove(activityNodeId);
            await _designer.UpdateActivityStatsAsync(activityId, Map(stats));
        }

        // Refreshed on the same cadence as the activity-keyed stats above: a gateway, an intermediate event or a
        // sequence flow has no activity id of its own, so it never appears in message.Stats, but the journal update
        // that produced this message is exactly what its own BPMN diagnostics ride along on.
        await _designer.RefreshElementStatsAsync();

        await InvokeAsync(StateHasChanged);

        // If we received an update for the selected activity, refresh the activity details.
        var selectedActivityNodeId = SelectedActivity?.GetNodeId();
        var includesSelectedActivity = selectedActivityNodeId != null && message.Stats.Any(x => x.ActivityNodeId == selectedActivityNodeId);

        if (includesSelectedActivity)
        {
            await HandleActivitySelectedAsync(SelectedActivity!);
        }
    }

    /// <summary>
    /// Starts the periodic elapsed-time timer, unless the component has already been disposed. Internal
    /// so tests can invoke it directly to pin the disposal race it guards against.
    /// </summary>
    internal void StartElapsedTimer()
    {
        if (_disposed) return;
        if (_elapsedTimer != null) return;

        async void Callback(object? _) => await ElapsedTimerTickAsync();

        // Create the timer disabled so its callback cannot fire before the timer is published to
        // _elapsedTimer; it is armed only after publication succeeds.
        var timer = new Timer(Callback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        if (Interlocked.CompareExchange(ref _elapsedTimer, timer, null) is not null)
        {
            // Another caller already installed a timer; discard the one we just created.
            timer.Dispose();
            return;
        }

        if (_disposed)
        {
            // DisposeAsync ran between the guard above and publishing the timer; detach and dispose it.
            var disposedTimer = Interlocked.Exchange(ref _elapsedTimer, null);
            disposedTimer?.Dispose();
            return;
        }

        try
        {
            timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }
        catch (ObjectDisposedException)
        {
            // The timer was disposed concurrently (e.g. by DisposeAsync racing this publish); nothing to arm.
        }
    }

    /// <summary>
    /// The body of the elapsed timer tick, extracted so tests can invoke it directly instead of
    /// waiting for the real <see cref="Timer"/> to fire.
    /// </summary>
    internal async Task ElapsedTimerTickAsync()
    {
        await RunTimerTickAsync(NotifyStateChangedAsync, StopElapsedTimerAsync);
    }

    /// <summary>
    /// Runs the body of a periodic timer tick, guarding it against disposal and a disconnected
    /// circuit. Returns <c>false</c> when the component was already disposed or when <paramref name="work"/>
    /// threw a circuit-gone exception (in which case <paramref name="stopTimer"/> stopped the timer);
    /// returns <c>true</c> when <paramref name="work"/> completed normally. Exceptions that do not
    /// signal a gone circuit propagate to the caller.
    /// </summary>
    private async Task<bool> RunTimerTickAsync(Func<Task> work, Func<Task> stopTimer)
    {
        if (_disposed) return false;

        try
        {
            await work();
        }
        catch (Exception ex) when (IsCircuitGoneException(ex))
        {
            // The circuit has disconnected (e.g. the browser tab hosting this workflow instance was
            // closed) while the tick was in flight. Stop the timer instead of letting the exception
            // escape the timer callback and crash the process.
            await stopTimer();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Invokes <see cref="ComponentBase.StateHasChanged"/> on the renderer's dispatcher. Extracted as
    /// a virtual seam so tests can simulate a circuit-gone exception surfacing from the render
    /// pipeline without needing a real Blazor circuit.
    /// </summary>
    internal virtual Task NotifyStateChangedAsync() => InvokeAsync(StateHasChanged);

    private void StopElapsedTimer()
    {
        var timer = Interlocked.Exchange(ref _elapsedTimer, null);
        timer?.Dispose();
    }

    private Task StopElapsedTimerAsync()
    {
        StopElapsedTimer();
        return Task.CompletedTask;
    }

    private async Task HandleActivitySelectedAsync(JsonObject activity)
    {
        await StopRefreshActivityStatePeriodically();
        await InvokeAsync(async () =>
        {
            var activityNodeId = activity.GetNodeId();
            SelectedActivity = activity;
            ActivityDescriptor = ActivityRegistry.Find(activity.GetTypeName(), activity.GetVersion());
            SelectedActivityExecutions = await GetActivityExecutionRecordsAsync(activityNodeId);
            StateHasChanged();
            _activityDetailsTab?.Refresh();
        });

        if (SelectedActivityExecutions.Any())
        {
            if (LastActivityExecution != null && (!LastActivityExecution.IsFused() || LastActivityExecution.Status == ActivityStatus.Running))
                RefreshActivityStatePeriodically(LastActivityExecution.Id);
        }
    }

    private async Task<ICollection<ActivityExecutionRecordSummary>> GetActivityExecutionRecordsAsync(string activityNodeId)
    {
        if (!_activityExecutionRecordsLookup.TryGetValue(activityNodeId, out var records))
        {
            records = (await ActivityExecutionService.ListSummariesAsync(WorkflowInstance!.Id, activityNodeId)).ToList();
            _activityExecutionRecordsLookup[activityNodeId] = records;
            _lastActivityExecutionRecordLookup.Remove(activityNodeId);
        }

        if (records.Any())
        {
            var lastRecord = records.Last();
            var lastActivityExecution = _lastActivityExecutionRecordLookup.GetValueOrDefault(activityNodeId);

            if (lastActivityExecution == null || lastActivityExecution.Id != lastRecord.Id)
            {
                lastActivityExecution = await ActivityExecutionService.GetAsync(lastRecord.Id);
                _lastActivityExecutionRecordLookup[activityNodeId] = lastActivityExecution!;
            }

            LastActivityExecution = lastActivityExecution;
        }
        else
        {
            LastActivityExecution = null;
        }

        return records;
    }

    private async Task UpdatePropertiesPaneHeightAsync()
    {
        var paneQuerySelector = $"#{ActivityPropertiesPane.UniqueID}";
        var visibleHeight = await DomAccessor.GetVisibleHeightAsync(paneQuerySelector);
        _propertiesPaneHeight = (int)visibleHeight - 50;
    }

    private async Task RefreshSelectedItemAsync(string activityExecutionRecordId)
    {
        if (LastActivityExecution != null)
        {
            _activityExecutionRecordsLookup.Remove(LastActivityExecution.ActivityNodeId);
            SelectedActivityExecutions = await GetActivityExecutionRecordsAsync(LastActivityExecution.ActivityNodeId);
        }

        await InvokeAsync(() =>
        {
            StateHasChanged();
            _activityDetailsTab?.Refresh();
        });
    }

    /// <summary>
    /// Starts the periodic activity-state refresh timer, unless the component has already been
    /// disposed. Internal so tests can invoke it directly to pin the disposal race it guards against.
    /// </summary>
    internal void RefreshActivityStatePeriodically(string activityExecutionRecordId)
    {
        if (_disposed) return;

        async void Callback(object? _) => await RefreshTimerTickAsync(activityExecutionRecordId);

        // Create the timer disabled so its callback cannot fire before the timer is published to
        // _refreshTimer; it is armed only after publication succeeds. Ownership of the timer created
        // here transfers to _refreshTimer via PublishRefreshTimer; it is disposed by the stop path
        // (StopRefreshActivityStatePeriodically) or by DisposeAsync.
        var timer = new Timer(Callback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        if (!PublishRefreshTimer(timer))
        {
            // PublishRefreshTimer already disposes the timer it detaches on this path; dispose it here
            // too so static analysis can see the local is disposed on every path (a second Timer.Dispose()
            // call is a safe no-op).
            timer.Dispose();
            return;
        }

        try
        {
            timer.Change(TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);
        }
        catch (ObjectDisposedException)
        {
            // The timer was disposed concurrently (e.g. by DisposeAsync racing this publish); nothing to arm.
        }
    }

    /// <summary>
    /// Publishes a newly created refresh timer to <see cref="_refreshTimer"/>, disposing any timer it
    /// replaces, and detaches the published timer again if the component was disposed concurrently.
    /// Returns <c>true</c> when <paramref name="timer"/> remains the published instance and should be
    /// armed; <c>false</c> when it was detached again and must not be armed.
    /// </summary>
    private bool PublishRefreshTimer(Timer timer)
    {
        var previousTimer = Interlocked.Exchange(ref _refreshTimer, timer);
        previousTimer?.Dispose();

        if (_disposed)
        {
            // DisposeAsync ran between the guard above and publishing the timer; detach and dispose it.
            var disposedTimer = Interlocked.Exchange(ref _refreshTimer, null);
            disposedTimer?.Dispose();
            return false;
        }

        return true;
    }

    /// <summary>
    /// The body of the periodic refresh timer tick, extracted so tests can invoke it directly instead
    /// of waiting for the real <see cref="Timer"/> to fire.
    /// </summary>
    internal async Task RefreshTimerTickAsync(string activityExecutionRecordId)
    {
        var ticked = await RunTimerTickAsync(() => RefreshSelectedItemAsync(activityExecutionRecordId), StopRefreshTimerAsync);

        if (!ticked) return;

        if (_disposed) return;

        if (LastActivityExecution == null || (LastActivityExecution.IsFused() && LastActivityExecution.Status != ActivityStatus.Running))
        {
            // Called from the tick itself: use the non-waiting stop so this callback does not await
            // its own completion (Timer.DisposeAsync waits for in-flight callbacks to return).
            StopRefreshTimer();
        }
        else
        {
            var timer = _refreshTimer;

            if (timer == null) return;

            try
            {
                timer.Change(TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException)
            {
                // The timer was disposed concurrently (e.g. by DisposeAsync racing this tick); nothing to rearm.
            }
        }
    }

    /// <summary>
    /// Stops the refresh timer without waiting for an in-flight callback to return. Use this from
    /// within the timer's own callback (<see cref="RefreshTimerTickAsync"/>): <see cref="Timer.DisposeAsync"/>
    /// only completes once active callbacks return, so awaiting it from the callback that is currently
    /// executing would deadlock the callback on its own completion.
    /// </summary>
    private void StopRefreshTimer()
    {
        var timer = Interlocked.Exchange(ref _refreshTimer, null);
        timer?.Dispose();
    }

    private Task StopRefreshTimerAsync()
    {
        StopRefreshTimer();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the refresh timer and drains any in-flight callback before returning. Use this only from
    /// callers that are not themselves executing on the timer callback (e.g. <see cref="DisposeAsync"/>
    /// or a non-timer caller), since <see cref="Timer.DisposeAsync"/> waits for active callbacks to
    /// finish.
    /// </summary>
    private async Task StopRefreshActivityStatePeriodically()
    {
        var timer = Interlocked.Exchange(ref _refreshTimer, null);

        if (timer is null) return;

        await timer.DisposeAsync();
    }

    /// <summary>
    /// Determines whether the given exception signals that the Blazor circuit is gone (disconnected
    /// or already disposed), in which case timer callbacks should stop quietly instead of throwing.
    /// </summary>
    private static bool IsCircuitGoneException(Exception ex) =>
        ex is ObjectDisposedException or JSDisconnectedException or OperationCanceledException;

    private static ActivityStats Map(ActivityExecutionStats source)
    {
        return new()
        {
            Faulted = source.IsFaulted,
            Blocked = source.IsBlocked,
            Completed = source.CompletedCount,
            Started = source.StartedCount,
            Uncompleted = source.UncompletedCount,
            Metadata = source.Metadata,
        };
    }

    private async Task OnActivitySelected(JsonObject activity)
    {
        await HandleActivitySelectedAsync(activity);

        var activitySelected = ActivitySelected;

        if (activitySelected.HasDelegate)
            await activitySelected.InvokeAsync(activity);
    }

    private async Task OnActivityExecutionSelected(string? executionId)
    {
        if (ActivityExecutionSelected.HasDelegate)
            await ActivityExecutionSelected.InvokeAsync(executionId);
    }

    private async Task OnResize(RadzenSplitterResizeEventArgs arg)
    {
        await UpdatePropertiesPaneHeightAsync();
    }

    private Task OnEditClicked()
    {
        var definitionId = WorkflowDefinition!.DefinitionId;

        if (SelectedSubWorkflow != null)
        {
            var typeName = SelectedSubWorkflow.GetTypeName();
            var version = SelectedSubWorkflow.GetVersion();
            var descriptor = ActivityRegistry.Find(typeName, version);
            var isWorkflowActivity = descriptor != null &&
                                     descriptor.CustomProperties.TryGetValue("RootType", out var rootTypeNameElement) &&
                                     ((JsonElement)rootTypeNameElement).GetString() == "WorkflowDefinitionActivity";
            if (isWorkflowActivity)
            {
                definitionId = SelectedSubWorkflow.GetWorkflowDefinitionId();
            }
        }

        var editWorkflowDefinition = EditWorkflowDefinition;

        if (editWorkflowDefinition.HasDelegate)
            return editWorkflowDefinition.InvokeAsync(definitionId);

        NavigationManager.NavigateTo($"workflows/definitions/{definitionId}/edit");
        return Task.CompletedTask;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        _disposed = true;
        StopElapsedTimer();

        try
        {
            await DisposeObserverAsync();
        }
        finally
        {
            await StopRefreshActivityStatePeriodically();
        }
    }
}
