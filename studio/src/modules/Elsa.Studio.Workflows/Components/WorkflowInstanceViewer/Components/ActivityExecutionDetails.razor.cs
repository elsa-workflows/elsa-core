using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.Resilience.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// <summary>
/// Displays the detailed properties of an activity execution record.
/// </summary>
public partial class ActivityExecutionDetails : IAsyncDisposable
{
    private Timer? _refreshTimer;

    /// <summary>
    /// The activity execution record to display details for.
    /// </summary>
    [Parameter] public ActivityExecutionRecord? ActivityExecution { get; set; }

    [Inject] private IActivityExecutionService ActivityExecutionService { get; set; } = null!;

    private DataPanelModel ActivityState { get; set; } = new();
    private DataPanelModel OutcomesData { get; set; } = new();
    private DataPanelModel OutputData { get; set; } = new();
    private PagedListResponse<RetryAttemptRecord> Retries { get; set; } = new();

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await StopRefreshTimerAsync();

        var record = ActivityExecution;
        if (record == null)
        {
            ActivityState = new();
            OutcomesData = new();
            OutputData = new();
            Retries = new();
            return;
        }

        CreateDataModels(record);
        await LoadRetriesAsync(record.Id);

        if (!record.IsFused())
            RefreshPeriodically(record.Id);
    }

    private void CreateDataModels(ActivityExecutionRecord? record)
    {
        if (record == null)
        {
            ActivityState = new();
            OutcomesData = new();
            OutputData = new();
            return;
        }

        var activityState = record.ActivityState?
            .Where(x => !x.Key.StartsWith("_"))
            .Select(x => new DataPanelItem(x.Key, x.Value?.ToString()))
            .ToDataPanelModel();

        var outcomesData = record.Payload?.TryGetValue("Outcomes", out var outcomesValue) == true
            ? new DataPanelModel { new DataPanelItem("Outcomes", outcomesValue!.ToString()!) }
            : null;

        var outputData = new DataPanelModel();

        if (record.Outputs != null)
            foreach (var (key, value) in record.Outputs)
                outputData.Add(key, value?.ToString());

        ActivityState = activityState ?? new();
        OutcomesData = outcomesData ?? new();
        OutputData = outputData;
    }

    private async Task LoadRetriesAsync(string id)
    {
        Retries = await ActivityExecutionService.GetRetriesAsync(id);
    }

    private async Task RefreshAsync(string id)
    {
        var record = await ActivityExecutionService.GetAsync(id);
        
        if (record == null)
        {
            await StopRefreshTimerAsync();
            return;
        }

        ActivityExecution = record;
        CreateDataModels(record);
        await LoadRetriesAsync(id);
        await InvokeAsync(StateHasChanged);

        if (record.IsFused())
            await StopRefreshTimerAsync();
    }

    private void RefreshPeriodically(string id)
    {
        async void Callback(object? _)
        {
            await RefreshAsync(id);
        }

        _refreshTimer = new Timer(Callback, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private async Task StopRefreshTimerAsync()
    {
        if (_refreshTimer == null) return;
        await _refreshTimer.DisposeAsync();
        _refreshTimer = null;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_refreshTimer != null) await _refreshTimer.DisposeAsync();
    }
}
