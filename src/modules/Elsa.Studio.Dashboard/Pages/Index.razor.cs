using Elsa.Studio.Authorization;
using Elsa.Studio.Contracts;
using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Services;
using Elsa.Studio.Dashboard.Widgets;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Dashboard.Pages;

public partial class Index : IAsyncDisposable
{
    // How long to wait for the features to report they are initialized before settling for the widgets there are: a host
    // that never initializes them (such as one without authorization) would otherwise leave the page loading forever.
    private static readonly TimeSpan FeatureInitializationTimeout = TimeSpan.FromSeconds(3);
    private static readonly IReadOnlyCollection<Permission> InstanceDataPermissions = DashboardPermissions.ForData(DashboardPermissions.WorkflowInstances);
    private static readonly IReadOnlyCollection<Permission> RuntimeDataPermissions = DashboardPermissions.ForData(DashboardPermissions.WorkflowRuntime);

    private ITimer? _initializationTimer;
    private IReadOnlyCollection<DashboardWidgetDescriptor> _permittedWidgets = [];
    private bool _widgetsSettled;
    private CancellationTokenSource? _loadCancellationTokenSource;
    private DataScope? _loadedScope;
    private UserPermissions? _loadedFor;
    private DashboardSnapshot? _snapshot;
    private DashboardLoadStatus _status = DashboardLoadStatus.Unavailable;
    private string _selectedRange = DashboardRangeKeys.TwentyFourHours;
    private string? _message;
    private bool _loading;
    private bool _disposed;
    private DateTimeOffset? _lastRefreshedAt;

    [Inject] private IDashboardService DashboardService { get; set; } = null!;
    [Inject] private IDashboardWidgetRegistry WidgetRegistry { get; set; } = null!;
    [Inject] private IEnumerable<DashboardWidgetDescriptor> Widgets { get; set; } = [];
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IFeatureService FeatureService { get; set; } = null!;
    [Inject] private TimeProvider TimeProvider { get; set; } = null!;

    /// <summary>The user's permissions, cascaded by the shell's page guard.</summary>
    [CascadingParameter] private UserPermissions Permissions { get; set; } = UserPermissions.Unknown;

    private DashboardWidgetContext WidgetContext => new(
        _selectedRange,
        _loading,
        _lastRefreshedAt,
        _status,
        _message,
        _snapshot,
        RefreshAsync,
        NavigationManager);

    // Core withholds the backend name from callers who may not read it: then there is no label to show.
    private string? BackendLabel
    {
        get
        {
            if (_snapshot == null)
                return "Selected backend";

            var overview = _snapshot.Overview;
            var names = new[] { overview.BackendName, overview.EnvironmentName }.Where(x => !string.IsNullOrWhiteSpace(x));
            return string.Join(" / ", names) is { Length: > 0 } label ? label : null;
        }
    }

    private string LastRefreshedLabel => _lastRefreshedAt == null ? "Not refreshed yet" : $"Refreshed {DashboardMetricFormatter.RelativeTimestamp(_lastRefreshedAt)}";

    private bool IsLoadingFirstSnapshot => _snapshot == null && (_loading || AwaitingWidgets);

    // Until the features are initialized (or we gave up waiting), the widgets they contribute may still be on their way.
    private bool WidgetsSettled => _widgetsSettled || FeatureService.IsInitialized;

    private bool AwaitingWidgets => _permittedWidgets.Count == 0 && !WidgetsSettled;

    // Without a widget to show, the user is welcomed with shortcuts (under the runtime status, where they may read it).
    private bool ShowWelcome => _permittedWidgets.Count == 0 && WidgetsSettled;

    private bool ShowRuntime =>
        _snapshot is { } snapshot
        && !snapshot.Overview.Runtime.Capability.IsUnauthorized
        && Permissions.HasAny(RuntimeDataPermissions);

    private string StatusLabel => _status switch
    {
        _ when IsLoadingFirstSnapshot => "Loading dashboard",
        DashboardLoadStatus.Unauthorized => "No access",
        DashboardLoadStatus.BackendDisconnected => "Backend disconnected",
        DashboardLoadStatus.Failed => "Refresh failed",
        DashboardLoadStatus.Loaded => "Loaded",
        _ => "Dashboard unavailable"
    };

    private Color StatusColor => IsLoadingFirstSnapshot ? Color.Default : Color.Error;

    private string StatusIcon => IsLoadingFirstSnapshot ? Icons.Material.Outlined.HourglassEmpty : Icons.Material.Outlined.CloudOff;

    private Severity AlertSeverity => _status switch
    {
        DashboardLoadStatus.Unauthorized => Severity.Warning,
        DashboardLoadStatus.Loaded => Severity.Info,
        _ => Severity.Error
    };

    // Worked out when the permissions or the registered widgets change, not on every access.
    private void RefreshPermittedWidgets() =>
        _permittedWidgets = Widgets
            .Concat(WidgetRegistry.List())
            .DistinctBy(x => x.Id)
            .Where(x => x.IsPermitted(Permissions))
            .ToList();

    // A widget needs the instance endpoints when it declares the workflow instances view permission (see the README).
    private static bool NeedsInstanceData(DashboardWidgetDescriptor widget) =>
        widget.RequiredPermissions.Contains(new(DashboardPermissions.WorkflowInstances, PermissionVerbs.View));

    // The data the permitted widgets need, limited to what the dashboard API serves this user: the workflow instance data
    // behind trends, activity, findings and hotspots only to a user who may read it and only when a widget needs it.
    // Without a widget there is nothing to load, except the overview for a user who may read the runtime status shown
    // above the welcome.
    private DataScope RequiredScope
    {
        get
        {
            if (_permittedWidgets.Count == 0)
                return WidgetsSettled && Permissions.HasAny(RuntimeDataPermissions) ? DataScope.Overview : DataScope.None;

            return _permittedWidgets.Any(NeedsInstanceData) && Permissions.HasAny(InstanceDataPermissions) ? DataScope.Everything : DataScope.Overview;
        }
    }

    private IReadOnlyCollection<DashboardWidgetDescriptor> GetWidgets(string zone) =>
        _permittedWidgets
            .Where(x => x.Zone == zone && x.IsVisible(WidgetContext))
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .ToList();

    // Subscribed before the first render, so widgets registered while it is on its way are not missed.
    protected override void OnInitialized()
    {
        FeatureService.Initialized += OnFeatureServiceInitialized;

        if (!FeatureService.IsInitialized)
            _initializationTimer = TimeProvider.CreateTimer(_ => _ = RefreshWidgetsAsync(), null, FeatureInitializationTimeout, Timeout.InfiniteTimeSpan);
    }

    // Loads on the first render, and again when a change in the user's permissions changes the data the page needs.
    protected override Task OnParametersSetAsync()
    {
        RefreshPermittedWidgets();
        return LoadIfScopeChangedAsync();
    }

    private void OnFeatureServiceInitialized() => _ = RefreshWidgetsAsync();

    // Widgets registered later than the wait still appear: the Initialized handler stays subscribed.
    private async Task RefreshWidgetsAsync()
    {
        if (_disposed)
            return;

        try
        {
            await InvokeAsync(async () =>
            {
                // Disposed while the callback was queued: a load started now would outlive the page.
                if (_disposed)
                    return;

                _widgetsSettled = true;
                _initializationTimer?.Dispose();
                RefreshPermittedWidgets();
                var loading = LoadIfScopeChangedAsync();
                StateHasChanged();
                await loading;

                if (!_disposed)
                    StateHasChanged();
            });
        }
        catch (Exception e) when (_disposed && e is InvalidOperationException or ObjectDisposedException or TaskCanceledException)
        {
        }
    }

    // The backend withholds sections by permission, so data loaded for other permissions is stale even when the same
    // endpoints are needed: swapping log permissions, for one, keeps the overview scope but changes what it returns.
    private async Task LoadIfScopeChangedAsync()
    {
        var scope = RequiredScope;

        if (scope != _loadedScope || scope != DataScope.None && !Permissions.IsEquivalentTo(_loadedFor))
            await RefreshAsync();
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
        var scope = RequiredScope;
        _loadedScope = scope;
        _loadedFor = Permissions;

        await CancelCurrentLoadAsync();

        // Without a widget to show there is nothing to load, and the user may not be allowed to load it anyway.
        if (scope == DataScope.None)
        {
            _loading = false;
            return;
        }

        var cancellationTokenSource = new CancellationTokenSource();
        _loadCancellationTokenSource = cancellationTokenSource;
        _loading = true;
        _message = null;

        try
        {
            var result = scope == DataScope.Everything
                ? await DashboardService.LoadAsync(range, cancellationToken: cancellationTokenSource.Token)
                : DashboardLoadResult.FromOverview(await DashboardService.LoadOverviewAsync(range, cancellationToken: cancellationTokenSource.Token));
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
        _initializationTimer?.Dispose();
        FeatureService.Initialized -= OnFeatureServiceInitialized;
        await CancelCurrentLoadAsync();
    }

    private enum DataScope
    {
        None,
        Overview,
        Everything
    }
}
