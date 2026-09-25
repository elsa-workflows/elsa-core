using Elsa.Studio.Contracts;
using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Services;
using Elsa.Studio.Dashboard.Widgets;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Dashboard.Pages;

public partial class Index : IAsyncDisposable
{
    private CancellationTokenSource? _loadCancellationTokenSource;
    private DashboardSnapshot? _snapshot;
    private DashboardLoadStatus _status = DashboardLoadStatus.Unavailable;
    private string _selectedRange = DashboardRangeKeys.TwentyFourHours;
    private string? _message;
    private bool _loading;
    private bool _disposed;
    private bool _subscribedToFeatureInitialized;
    private DateTimeOffset? _lastRefreshedAt;

    [Inject] private IDashboardService DashboardService { get; set; } = null!;
    [Inject] private IDashboardWidgetRegistry WidgetRegistry { get; set; } = null!;
    [Inject] private IEnumerable<DashboardWidgetDescriptor> Widgets { get; set; } = [];
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IFeatureService FeatureService { get; set; } = null!;

    private DashboardWidgetContext WidgetContext => new(
        _selectedRange,
        _loading,
        _lastRefreshedAt,
        _status,
        _message,
        _snapshot,
        RefreshAsync,
        NavigationManager);

    private string BackendLabel
    {
        get
        {
            if (_snapshot == null)
                return "Selected backend";

            var overview = _snapshot.Overview;
            var backendName = string.IsNullOrWhiteSpace(overview.BackendName) ? "Backend" : overview.BackendName;
            return string.IsNullOrWhiteSpace(overview.EnvironmentName) ? backendName : $"{backendName} / {overview.EnvironmentName}";
        }
    }

    private string LastRefreshedLabel => _lastRefreshedAt == null ? "Not refreshed yet" : $"Refreshed {DashboardMetricFormatter.RelativeTimestamp(_lastRefreshedAt)}";

    private string StatusLabel => _status switch
    {
        DashboardLoadStatus.Unauthorized => "No access",
        DashboardLoadStatus.BackendDisconnected => "Backend disconnected",
        DashboardLoadStatus.Failed => "Refresh failed",
        DashboardLoadStatus.Loaded => "Loaded",
        _ => "Dashboard unavailable"
    };

    private Severity AlertSeverity => _status switch
    {
        DashboardLoadStatus.Unauthorized => Severity.Warning,
        DashboardLoadStatus.Loaded => Severity.Info,
        _ => Severity.Error
    };

    private IReadOnlyCollection<DashboardWidgetDescriptor> GetWidgets(string zone) =>
        Widgets
            .Concat(WidgetRegistry.List())
            .DistinctBy(x => x.Id)
            .Where(x => x.Zone == zone && x.IsVisible(WidgetContext))
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .ToList();

    protected override async Task OnInitializedAsync()
    {
        await RefreshAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender || _disposed)
            return;

        FeatureService.Initialized += OnFeatureServiceInitialized;
        _subscribedToFeatureInitialized = true;
    }

    private void OnFeatureServiceInitialized()
    {
        _ = RefreshWidgetsAfterFeatureInitializationAsync();
    }

    private async Task RefreshWidgetsAfterFeatureInitializationAsync()
    {
        if (_disposed)
            return;

        try
        {
            await InvokeAsync(StateHasChanged);
        }
        catch (InvalidOperationException) when (_disposed)
        {
        }
    }

    private async Task OnRangeChangedAsync(string? range)
    {
        var selectedRange = DashboardRangeMapper.Normalize(range);

        if (_selectedRange == selectedRange)
            return;

        _selectedRange = selectedRange;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        await LoadAsync(_selectedRange);
    }

    private async Task LoadAsync(string range)
    {
        await CancelCurrentLoadAsync();

        var cancellationTokenSource = new CancellationTokenSource();
        _loadCancellationTokenSource = cancellationTokenSource;
        _loading = true;
        _message = null;

        try
        {
            var result = await DashboardService.LoadAsync(range, cancellationToken: cancellationTokenSource.Token);
            _status = result.Status;

            if (result.Snapshot != null)
            {
                _snapshot = result.Snapshot;
                _lastRefreshedAt = DateTimeOffset.UtcNow;
                _message = null;
            }
            else
            {
                _message = result.Message;
            }
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception e)
        {
            _status = DashboardLoadStatus.Failed;
            _message = e.Message;
        }
        finally
        {
            if (ReferenceEquals(_loadCancellationTokenSource, cancellationTokenSource))
            {
                _loading = false;
                _loadCancellationTokenSource = null;
            }

            cancellationTokenSource.Dispose();
        }
    }

    private async Task CancelCurrentLoadAsync()
    {
        if (_loadCancellationTokenSource == null)
            return;

        await _loadCancellationTokenSource.CancelAsync();
        _loadCancellationTokenSource = null;
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;

        if (_subscribedToFeatureInitialized)
            FeatureService.Initialized -= OnFeatureServiceInitialized;

        await CancelCurrentLoadAsync();
    }
}
