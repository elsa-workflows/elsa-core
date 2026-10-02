using Elsa.Api.Client.Resources.Alterations.Contracts;
using Elsa.Api.Client.Resources.Alterations.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Constants;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Components.WorkflowInstanceList.Components;
using Elsa.Studio.Workflows.Components.WorkflowInstanceList.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.JSInterop;
using MudBlazor;
using Refit;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceList;

/// Represents the workflow instances list page.
public partial class WorkflowInstanceList : IAsyncDisposable
{
    private Task _loadWorkflowsTask = Task.CompletedTask;
    private DotNetObjectReference<WorkflowInstanceList>? _dotNetRef;
    private MudTable<WorkflowInstanceRow> _table = null!;
    private HashSet<WorkflowInstanceRow> _selectedRows = new();
    private int _totalCount;
    private Timer? _elapsedTimer;

    /// <summary>
    /// An event that is invoked when a workflow definition is edited.
    /// </summary>
    [Parameter] public EventCallback<string> ViewWorkflowInstance { get; set; }

    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IUserMessageService UserMessageService { get; set; } = default!;
    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private IBackendApiClientProvider BackendApiClientProvider { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private IFiles Files { get; set; } = default!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = default!;
    [Inject] private new ILogger<WorkflowInstanceList> Logger { get; set; } = default!;
    [Inject] private IOptions<WorkflowInstanceListPollingOptions> PollingOptions { get; set; } = default!;
    [Inject] private new NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IRemoteFeatureProvider RemoteFeatureProvider { get; set; } = default!;

    /// <summary>
    /// Invoked when the "Ctrl+R" hotkeys are pressed, triggering the refresh operation.
    /// </summary>
    [JSInvokable] public void OnHotKeysCtrlR() => _table.ReloadServerData();

    private ICollection<WorkflowDefinitionSummary> WorkflowDefinitions { get; set; } = [];
    private ICollection<WorkflowDefinitionSummary> SelectedWorkflowDefinitions { get; set; } = [];

    /// The selected statuses to filter by.
    private ICollection<WorkflowStatus> SelectedStatuses { get; set; } = [];

    /// The selected sub-statuses to filter by.
    private ICollection<WorkflowSubStatus> SelectedSubStatuses { get; set; } = [];

    // The selected timestamp filters to filter by.
    private List<TimestampFilterModel> TimestampFilters { get; set; } = [];

    private string SearchTerm { get; set; } = string.Empty;
    private bool EnablePolling { get; set; }
    private bool? HasIncidents { get; set; }
    private bool IsDateRangePopoverOpen { get; set; }
    private bool IsAlterationsEnabled { get; set; }

    private void Reload() => _table.ReloadServerData();
    private async Task ViewAsync(string instanceId) => await ViewWorkflowInstance.InvokeAsync(instanceId);
    private string? GetWorkflowDefinitionDisplayText(WorkflowDefinitionSummary? definition) => definition?.Name;
    private void ToggleDateRangePopover() => IsDateRangePopoverOpen = !IsDateRangePopoverOpen;
    private void OnViewClicked(string instanceId) => _ = ViewAsync(instanceId);
    private void OnRowClick(TableRowClickEventArgs<WorkflowInstanceRow> e) => _ = ViewAsync(e.Item!.WorkflowInstanceId);
    private Task OnImportClicked() => DomAccessor.ClickElementAsync("#instance-file-upload-button-wrapper input[type=file]");

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        EnablePolling = PollingOptions.Value.IsEnabledByDefault;
        IsAlterationsEnabled = await RemoteFeatureProvider.IsEnabledOrDefaultAsync(RemoteFeatureNames.Alterations);
        _loadWorkflowsTask = LoadWorkflowDefinitionsAsync();
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("editorHotkeys.register", _dotNetRef);
            StartElapsedTimer();
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task LoadWorkflowDefinitionsAsync()
    {
        var workflowDefinitionsResponse = await WorkflowDefinitionService.ListAsync(new(), VersionOptions.LatestOrPublished);
        var workflowDefinitions = workflowDefinitionsResponse.Items;

        // Filter the definitions to ensure only the one with the highest version for each DefinitionId remains
        var filteredWorkflowDefinitions = workflowDefinitions
            .GroupBy(definition => definition.DefinitionId) // Group by DefinitionId
            .Select(group => group.OrderByDescending(definition => definition.Version).First()) // Get the highest version in each group
            .ToList();

        WorkflowDefinitions = filteredWorkflowDefinitions;
    }

    private async Task<TableData<WorkflowInstanceRow>> LoadData(TableState state, CancellationToken cancellationToken)
    {
        var request = new ListWorkflowInstancesRequest
        {
            Page = state.Page,
            PageSize = state.PageSize,
            DefinitionIds = SelectedWorkflowDefinitions.Select(x => x.DefinitionId).ToList(),
            Statuses = SelectedStatuses,
            SubStatuses = SelectedSubStatuses,
            SearchTerm = SearchTerm,
            HasIncidents = HasIncidents,
            IsSystem = false,
            OrderBy = GetOrderBy(state.SortLabel),
            OrderDirection = state.SortDirection == SortDirection.Descending ? OrderDirection.Descending : OrderDirection.Ascending,
            TimestampFilters = TimestampFilters.Select(Map).Where(x => x.Timestamp.Date > DateTime.MinValue && !string.IsNullOrWhiteSpace(x.Column)).ToList()
        };

        try
        {
            var workflowInstancesResponse = await WorkflowInstanceService.ListAsync(request, cancellationToken);
            var definitionVersionIds = workflowInstancesResponse.Items.Select(x => x.DefinitionVersionId).Distinct().ToList();

            var workflowDefinitionVersionsResponse = await WorkflowDefinitionService.ListAsync(new()
            {
                Ids = definitionVersionIds,
            }, cancellationToken: cancellationToken);

            var workflowDefinitionVersionsLookup = workflowDefinitionVersionsResponse.Items.ToDictionary(x => x.Id);

            // Select any workflow instances for which no corresponding workflow definition version was found.
            // This can happen when a workflow definition is deleted.
            var missingWorkflowDefinitionVersionIds = definitionVersionIds.Except(workflowDefinitionVersionsLookup.Keys).ToList();
            var filteredWorkflowInstances = workflowInstancesResponse.Items.Where(x => !missingWorkflowDefinitionVersionIds.Contains(x.DefinitionVersionId));

            var rows = filteredWorkflowInstances.Select(x => new WorkflowInstanceRow(
                x.Id,
                x.CorrelationId,
                workflowDefinitionVersionsLookup[x.DefinitionVersionId]?.Name ?? string.Empty,
                x.Version,
                x.Name,
                x.Status,
                x.SubStatus,
                x.IncidentCount,
                x.CreatedAt,
                x.UpdatedAt,
                x.FinishedAt));

            _totalCount = (int)workflowInstancesResponse.TotalCount;

            // Update URL to reflect current table state & filters
            TryUpdateUrlFromState(state);

            return new() { TotalItems = _totalCount, Items = rows };
        }
        catch (TaskCanceledException)
        {
            Logger.LogWarning("Failed to list workflow instances due to a cancellation.");
            _totalCount = 0;
            return new() { TotalItems = 0, Items = Array.Empty<WorkflowInstanceRow>() };
        }
        catch (ApiException ex) when (ex.InnerException is TaskCanceledException)
        {
            Logger.LogWarning("Failed to list workflow instances due to a cancellation.");
            _totalCount = 0;
            return new() { TotalItems = 0, Items = Array.Empty<WorkflowInstanceRow>() };
        }
    }

    /// <inheritdoc/>
    protected override async Task ApplyQueryParameters(IDictionary<string, string> query)
    {
        // Paging
        if (query.TryGetValue("page", out var pageValue) && int.TryParse(pageValue, out var page)) InitialPage = page - 1;
        if (query.TryGetValue("pageSize", out var pageSizeValue) && int.TryParse(pageSizeValue, out var pageSize)) InitialPageSize = pageSize;

        // Filters
        if (query.TryGetValue("search", out var searchValues)) SearchTerm = searchValues;
        if (query.TryGetValue("hasIncidents", out var incidentsValues) && bool.TryParse(incidentsValues, out var incidents)) HasIncidents = incidents;
        if (query.TryGetValue("statuses", out var statusesValues) && !StringValues.IsNullOrEmpty(statusesValues))
        {
            SelectedStatuses = statusesValues
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Enum.TryParse<WorkflowStatus>(s, true, out var st) ? (WorkflowStatus?)st : null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();
        }
        if (query.TryGetValue("substatuses", out var substatusesValues) && !StringValues.IsNullOrEmpty(substatusesValues))
        {
            SelectedSubStatuses = substatusesValues
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Enum.TryParse<WorkflowSubStatus>(s, true, out var st) ? (WorkflowSubStatus?)st : null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();
        }
        if (query.TryGetValue("defs", out var defsValues) && !StringValues.IsNullOrEmpty(defsValues))
        {
            // Ensure WorkflowDefinitions are loaded
            await _loadWorkflowsTask;

            var ids = defsValues
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            SelectedWorkflowDefinitions = WorkflowDefinitions.Where(wd => ids.Contains(wd.DefinitionId)).ToList();
        }
        if (query.TryGetValue("ts", out var tsValues) && !StringValues.IsNullOrEmpty(tsValues))
        {
            var raw = tsValues;
            try
            {
                var bytes = Convert.FromBase64String(raw);
                var json = Encoding.UTF8.GetString(bytes);
                var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
                var decoded = JsonSerializer.Deserialize<List<TimestampFilterModel>>(json, opts);
                TimestampFilters = decoded ?? new List<TimestampFilterModel>();
            }
            catch
            {
                TimestampFilters = [];
            }
        }
    }

    /// <inheritdoc/>
    protected override Dictionary<string, string?> BuildQueryFromState(TableState state)
    {
        var query = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["page"] = (state.Page + 1).ToString(),
            ["pageSize"] = state.PageSize.ToString()
        };

        if (!string.IsNullOrWhiteSpace(SearchTerm)) query["search"] = SearchTerm;
        if (HasIncidents != null) query["hasIncidents"] = HasIncidents.ToString();
        if (SelectedStatuses.Any()) query["statuses"] = string.Join(',', SelectedStatuses.Select(s => s.ToString()));
        if (SelectedSubStatuses.Any()) query["substatuses"] = string.Join(',', SelectedSubStatuses.Select(s => s.ToString()));
        if (SelectedWorkflowDefinitions.Any()) query["defs"] = string.Join(',', SelectedWorkflowDefinitions.Select(d => d.DefinitionId));
        if (TimestampFilters.Any()) query["ts"] = EncodeTimestampFiltersToBase64Json(TimestampFilters); // Encode timestamp filters as JSON then base64 to avoid delimiter collisions

        return query;
    }

    private async Task<PagedListResponse<WorkflowInstanceSummary>> TryListWorkflowDefinitionsAsync(ListWorkflowInstancesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await WorkflowInstanceService.ListAsync(request, cancellationToken);
        }
        catch (ApiException ex) when (ex.InnerException is TaskCanceledException)
        {
            Logger.LogWarning("Failed to list workflow instances due to a timeout.");
            return new()
            {
                Items = Array.Empty<WorkflowInstanceSummary>()
            };
        }
    }


    private TimestampFilter Map(TimestampFilterModel source)
    {
        var date = !string.IsNullOrWhiteSpace(source.Date) ? DateTime.Parse(source.Date) : DateTime.MinValue;
        var time = !string.IsNullOrWhiteSpace(source.Time) ? TimeSpan.Parse(source.Time) : TimeSpan.Zero;
        var dateTime = date.Add(time);
        var timestamp = dateTime == DateTime.MinValue ? DateTimeOffset.MinValue : new(dateTime);

        return new()
        {
            Column = source.Column,
            Operator = source.Operator,
            Timestamp = timestamp
        };
    }

    private OrderByWorkflowInstance? GetOrderBy(string? sortLabel)
    {
        return sortLabel switch
        {
            "Name" => OrderByWorkflowInstance.Name,
            "Finished" => OrderByWorkflowInstance.Finished,
            "Created" => OrderByWorkflowInstance.Created,
            "LastExecuted" => OrderByWorkflowInstance.LastExecuted,
            _ => null
        };
    }

    private bool FilterWorkflowDefinitions(WorkflowDefinitionSummary workflowDefinition, string term)
    {
        var trimmedTerm = term.Trim();

        if (string.IsNullOrEmpty(term))
            return true;

        var sources = new[]
        {
            (string?)workflowDefinition.Name
        };

        return sources.Any(x => x?.Contains(trimmedTerm, StringComparison.OrdinalIgnoreCase) == true);
    }

    private Color GetSubStatusColor(WorkflowSubStatus subStatus)
    {
        return subStatus switch
        {
            WorkflowSubStatus.Pending => Color.Tertiary,
            WorkflowSubStatus.Suspended => Color.Warning,
            WorkflowSubStatus.Finished => Color.Success,
            WorkflowSubStatus.Faulted => Color.Error,
            WorkflowSubStatus.Cancelled => Color.Tertiary,
            WorkflowSubStatus.Executing => Color.Primary,
            _ => Color.Default,
        };
    }

    private async Task OnDeleteClicked(WorkflowInstanceRow row)
    {
        var result = await DialogService.ShowMessageBoxAsync(Localizer["Delete workflow instance?"], Localizer["Are you sure you want to delete this workflow instance?"], yesText: Localizer["Delete"], cancelText: Localizer["Cancel"]);

        if (result != true)
            return;

        var instanceId = row.WorkflowInstanceId;
        await WorkflowInstanceService.DeleteAsync(instanceId);
        Reload();
    }

    private void OnAlterClicked(WorkflowInstanceRow row)
    {
        NavigationManager.NavigateTo($"alterations/instances/{row.WorkflowInstanceId}");
    }

    private async Task OnCancelClicked(WorkflowInstanceRow row)
    {
        var result = await DialogService.ShowMessageBoxAsync(Localizer["Cancel workflow instance?"], Localizer["Are you sure you want to cancel this workflow instance?"], yesText: Localizer["Yes"], cancelText: Localizer["No"]);

        if (result != true)
            return;

        var instanceId = row.WorkflowInstanceId;
        await WorkflowInstanceService.CancelAsync(instanceId);
        Reload();
    }

    private async Task OnDownloadClicked(WorkflowInstanceRow workflowInstanceRow)
    {
        var download = await WorkflowInstanceService.ExportAsync(workflowInstanceRow.WorkflowInstanceId);
        var fileName = $"{workflowInstanceRow.Name?.Kebaberize() ?? workflowInstanceRow.WorkflowInstanceId}.json";
        await Files.DownloadFileFromStreamAsync(fileName, download.Content);
    }

    private async Task OnBulkDeleteClicked()
    {
        var result = await DialogService.ShowMessageBoxAsync(Localizer["Delete selected workflow instances?"], Localizer["Are you sure you want to delete the selected workflow instances?"], yesText: Localizer["Delete"], cancelText: Localizer["Cancel"]);

        if (result != true)
            return;

        var workflowInstanceIds = _selectedRows.Select(x => x.WorkflowInstanceId).ToList();
        await WorkflowInstanceService.BulkDeleteAsync(workflowInstanceIds);
        _selectedRows.Clear();
        Reload();
    }

    private async Task OnBulkCancelClicked()
    {
        var reference = await DialogService.ShowAsync<BulkCancelDialog>(Localizer["Cancel selected workflow instances?"]);
        var dialogResult = await reference.Result;

        if (dialogResult == null || dialogResult.Canceled)
            return;

        var applyToAllMatches = (bool)(dialogResult.Data ?? false);

        if (applyToAllMatches)
        {
            var cancel = new JsonObject
            {
                ["type"] = "Cancel"
            };

            var plan = new AlterationPlanParams
            {
                Alterations = [cancel],
                Filter = new()
                {
                    EmptyFilterSelectsAll = true,
                    HasIncidents = HasIncidents,
                    IsSystem = false,
                    SearchTerm = SearchTerm,
                    Statuses = SelectedStatuses.Any() ? SelectedStatuses : null,
                    SubStatuses = SelectedSubStatuses.Any() ? SelectedSubStatuses : null,
                    TimestampFilters = TimestampFilters.Any() ? TimestampFilters.Select(Map).Where(x => x.Timestamp.Date > DateTime.MinValue && !string.IsNullOrWhiteSpace(x.Column)).ToList() : null,
                    DefinitionIds = SelectedWorkflowDefinitions.Any() ? SelectedWorkflowDefinitions.Select(x => x.DefinitionId).ToList() : null
                }
            };

            var alterationsApi = await BackendApiClientProvider.GetApiAsync<IAlterationsApi>();
            await alterationsApi.Submit(plan);
            UserMessageService.ShowSnackbarTextMessage("Workflow instances are being cancelled.", Severity.Info, options => { options.SnackbarVariant = Variant.Filled; });
        }
        else
        {
            var workflowInstanceIds = _selectedRows.Select(x => x.WorkflowInstanceId).ToList();
            var request = new BulkCancelWorkflowInstancesRequest
            {
                Ids = workflowInstanceIds
            };
            await WorkflowInstanceService.BulkCancelAsync(request);
        }

        Reload();
    }

    private async Task OnBulkExportClicked()
    {
        var workflowInstanceIds = _selectedRows.Select(x => x.WorkflowInstanceId).ToList();
        var download = await WorkflowInstanceService.BulkExportAsync(workflowInstanceIds);
        var fileName = download.FileName;
        await Files.DownloadFileFromStreamAsync(fileName, download.Content);
    }

    private async Task OnFilesSelected(IReadOnlyList<IBrowserFile> files)
    {
        var maxAllowedSize = 1024 * 1024 * 10; // 10 MB
        var streamParts = files.Select(x => new StreamPart(x.OpenReadStream(maxAllowedSize), x.Name, x.ContentType)).ToList();
        var count = await WorkflowInstanceService.BulkImportAsync(streamParts);
        var message = count == 1 ? Localizer["Successfully imported one instance"] : Localizer["Successfully imported {0} instances", count];
        UserMessageService.ShowSnackbarTextMessage(message, Severity.Success, options => { options.SnackbarVariant = Variant.Filled; });
        Reload();
    }


    private async Task OnSelectedWorkflowDefinitionsChanged(IEnumerable<WorkflowDefinitionSummary> values)
    {
        SelectedWorkflowDefinitions = values.ToList();
        await _table.ReloadServerData();
    }

    private async Task OnSelectedStatusesChanged(IEnumerable<WorkflowStatus> values)
    {
        SelectedStatuses = values.ToList();
        await _table.ReloadServerData();
    }

    private async Task OnSelectedSubStatusesChanged(IEnumerable<WorkflowSubStatus> values)
    {
        SelectedSubStatuses = values.ToList();
        await _table.ReloadServerData();
    }

    private async Task OnSearchTermChanged(string text)
    {
        if (text != SearchTerm)
        {
            SearchTerm = text;
            await _table.ReloadServerData();
        }
    }

    private async Task OnHasIncidentsChanged(bool? value)
    {
        HasIncidents = value;
        await _table.ReloadServerData();
    }

    private void OnAddTimestampFilterClicked()
    {
        TimestampFilters.Add(new());
        StateHasChanged();
    }

    private void OnRemoveTimestampFilterClicked(TimestampFilterModel filter)
    {
        TimestampFilters.Remove(filter);
        StateHasChanged();
    }

    private void OnClearTimestampFiltersClicked()
    {
        TimestampFilters.Clear();
        StateHasChanged();
    }

    private async Task OnApplyTimestampFiltersClicked()
    {
        await _table.ReloadServerData();
        ToggleDateRangePopover();
    }

    private void OnPollingChanged(bool enabled)
    {
        EnablePolling = enabled;

        if (EnablePolling)
            StartElapsedTimer();
        else
            StopElapsedTimer();
    }

    private void StartElapsedTimer()
    {
        if (!EnablePolling)
            return;

        _elapsedTimer ??= new(_ => InvokeAsync(async () => await _table.ReloadServerData()), null, TimeSpan.FromSeconds(PollingOptions.Value.IntervalSeconds), TimeSpan.FromSeconds(PollingOptions.Value.IntervalSeconds));
    }

    private void StopElapsedTimer()
    {
        if (_elapsedTimer != null)
        {
            _elapsedTimer.Dispose();
            _elapsedTimer = null;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        StopElapsedTimer();
        if (_dotNetRef != null)
        {
            await JSRuntime.InvokeVoidAsync("editorHotkeys.dispose", _dotNetRef);
            _dotNetRef.Dispose();
            _dotNetRef = null;
        }
    }
}
