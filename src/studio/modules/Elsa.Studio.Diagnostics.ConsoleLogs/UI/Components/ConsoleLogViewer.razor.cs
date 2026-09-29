using Elsa.Studio.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Studio.Diagnostics.ConsoleLogs.Models;
using Elsa.Studio.Diagnostics.ConsoleLogs.Services;
using Elsa.Studio.Diagnostics.Rendering;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Refit;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.UI.Components;

/// <summary>
/// Displays diagnostics console logs.
/// </summary>
[UsedImplicitly]
public partial class ConsoleLogViewer : IAsyncDisposable
{
    private readonly string _logSurfaceId = $"console-logs-surface-{Guid.NewGuid():N}";
    private readonly List<ConsoleLogSource> _sources = new();
    private readonly Queue<ConsoleLogLine> _refreshBufferedLines = new();
    private readonly ConditionalWeakTable<ConsoleLogLine, ParsedConsoleLogText> _parsedTextCache = new();
    private CancellationTokenSource _cancellationTokenSource = new();
    private IJSObjectReference? _scriptModule;
    private bool _queryInitialized;
    private bool _isRefreshing;
    private bool _scrollAfterRender;
    private bool _activated;
    private string? _appliedWorkflowInstanceId;
    private string? _appliedActivityInstanceId;
    private string? _appliedActivityId;
    private string? _appliedActivityNodeId;
    private long _refreshVersion;
    private bool _refreshAfterActivation;

    [Inject] private IConsoleLogService ConsoleLogService { get; set; } = default!;
    [Inject] private IConsoleLogObserver Observer { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ConsoleLogExportFormatter ExportFormatter { get; set; } = default!;
    [Inject] private ConsoleLogUrlStateMapper UrlStateMapper { get; set; } = default!;

    /// <summary>
    /// Gets or sets whether filter controls are shown.
    /// </summary>
    [Parameter] public bool ShowFilters { get; set; } = true;

    /// <summary>
    /// Gets or sets whether viewer state is read from and written to the current URL.
    /// </summary>
    [Parameter] public bool UseUrlState { get; set; }

    /// <summary>
    /// Gets or sets the workflow instance ID to scope console lines to.
    /// </summary>
    [Parameter] public string? WorkflowInstanceId { get; set; }

    /// <summary>
    /// Gets or sets the activity execution instance ID to scope console lines to.
    /// </summary>
    [Parameter] public string? ActivityInstanceId { get; set; }

    /// <summary>
    /// Gets or sets the logical activity ID to scope console lines to.
    /// </summary>
    [Parameter] public string? ActivityId { get; set; }

    /// <summary>
    /// Gets or sets the activity node ID to scope console lines to.
    /// </summary>
    [Parameter] public string? ActivityNodeId { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of visible rows to keep locally.
    /// </summary>
    [Parameter] public int? VisibleRowCap { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes for the viewer root.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// Gets or sets inline styles for the viewer root.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    protected ConsoleLogViewState ViewState { get; } = new();
    protected IReadOnlyCollection<ConsoleLogLine> Rows => ViewState.VisibleRows;
    protected IReadOnlyList<ConsoleLogSource> Sources => _sources;
    protected string LogSurfaceId => _logSurfaceId;
    protected string RootCssClass => string.Join(" ", new[] { "console-log-viewer", Class }.Where(x => !string.IsNullOrWhiteSpace(x)));
    protected string? RootStyle => Style;
    protected bool IsLoading { get; private set; }
    protected string? ErrorMessage { get; private set; }
    protected long BackendDroppedCount { get; private set; }
    protected long DiscardedRefreshBufferRows { get; private set; }
    protected string StreamSelection => ConsoleLogUrlStateMapper.FormatStream(ViewState.Filter.Stream);
    protected string StatusText => ViewState.ConnectionStatus switch
    {
        ConsoleLogConnectionStatus.Connecting => "Connecting",
        ConsoleLogConnectionStatus.Connected => ViewState.IsPaused ? "Paused" : "Connected",
        ConsoleLogConnectionStatus.Reconnecting => "Reconnecting",
        ConsoleLogConnectionStatus.Unavailable => "Unavailable",
        ConsoleLogConnectionStatus.Unauthorized => "Unauthorized",
        ConsoleLogConnectionStatus.Error => "Error",
        _ => "Disconnected"
    };
    protected string StatusCssClass => $"console-logs-status-{ViewState.ConnectionStatus.ToString().ToLowerInvariant()}{(ViewState.IsPaused ? " console-logs-status-paused" : "")}";
    protected string PauseIcon => ViewState.IsPaused ? Icons.Material.Filled.PlayArrow : Icons.Material.Filled.Pause;
    protected string LogSurfaceCssClass => $"console-logs-surface{(ViewState.Wrap ? " console-logs-wrap" : "")}{(ViewState.Compact ? " console-logs-compact" : "")}";
    protected bool HasActiveFilter => !string.IsNullOrWhiteSpace(ViewState.Filter.SourceId) ||
                                      !string.IsNullOrWhiteSpace(ViewState.Filter.Query) ||
                                      !string.IsNullOrWhiteSpace(ViewState.Filter.WorkflowInstanceId) ||
                                      !string.IsNullOrWhiteSpace(ViewState.Filter.ActivityInstanceId) ||
                                      !string.IsNullOrWhiteSpace(ViewState.Filter.ActivityId) ||
                                      !string.IsNullOrWhiteSpace(ViewState.Filter.ActivityNodeId) ||
                                      !string.Equals(StreamSelection, "both", StringComparison.Ordinal);
    protected string EmptyText => HasActiveFilter ? "No console lines match the current filters." : "No console lines received yet.";
    protected string? StateMessage => ViewState.ConnectionStatus switch
    {
        ConsoleLogConnectionStatus.Unauthorized => "You do not have permission to view diagnostics console logs.",
        ConsoleLogConnectionStatus.Unavailable => "Diagnostics console logs are not available on this backend.",
        ConsoleLogConnectionStatus.Error => "The diagnostics console stream encountered an error.",
        ConsoleLogConnectionStatus.Disconnected when Rows.Count == 0 => "The live diagnostics console stream is disconnected.",
        ConsoleLogConnectionStatus.Reconnecting => "Reconnecting to the live diagnostics console stream.",
        _ => null
    };
    protected Severity StateSeverity => ViewState.ConnectionStatus switch
    {
        ConsoleLogConnectionStatus.Unauthorized => Severity.Error,
        ConsoleLogConnectionStatus.Error => Severity.Error,
        ConsoleLogConnectionStatus.Unavailable => Severity.Warning,
        ConsoleLogConnectionStatus.Disconnected => Severity.Warning,
        _ => Severity.Info
    };

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (VisibleRowCap.HasValue)
            ViewState.VisibleRowCap = VisibleRowCap.Value;

        ApplyScopeParameters();
        _refreshAfterActivation = false;

        if (UseUrlState)
            ApplyQueryFromUrl();

        Observer.LineReceived += OnLineReceivedAsync;
        Observer.DroppedLinesReceived += OnDroppedLinesReceivedAsync;
        Observer.ConnectionStatusChanged += OnConnectionStatusChangedAsync;
        Observer.SourceChanged += OnSourceChangedAsync;
        await ActivateAsync(_cancellationTokenSource.Token);
        _activated = true;

        if (_refreshAfterActivation)
        {
            _refreshAfterActivation = false;
            await RefreshFilterAsync();
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var workflowInstanceId = NormalizeScopeValue(WorkflowInstanceId);
        var activityInstanceId = NormalizeScopeValue(ActivityInstanceId);
        var activityId = NormalizeScopeValue(ActivityId);
        var activityNodeId = NormalizeScopeValue(ActivityNodeId);

        if (HasAppliedScope(workflowInstanceId, activityInstanceId, activityId, activityNodeId))
            return;

        ApplyScopeParameters();

        if (!_activated)
        {
            _refreshAfterActivation = true;
            return;
        }

        await RefreshFilterAsync();
    }

    private bool HasAppliedScope(string? workflowInstanceId, string? activityInstanceId, string? activityId, string? activityNodeId) =>
        string.Equals(_appliedWorkflowInstanceId, workflowInstanceId, StringComparison.Ordinal) &&
            string.Equals(_appliedActivityInstanceId, activityInstanceId, StringComparison.Ordinal) &&
            string.Equals(_appliedActivityId, activityId, StringComparison.Ordinal) &&
            string.Equals(_appliedActivityNodeId, activityNodeId, StringComparison.Ordinal);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_scrollAfterRender)
            return;

        _scrollAfterRender = false;
        await ScrollToBottomAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Observer.LineReceived -= OnLineReceivedAsync;
        Observer.DroppedLinesReceived -= OnDroppedLinesReceivedAsync;
        Observer.ConnectionStatusChanged -= OnConnectionStatusChangedAsync;
        Observer.SourceChanged -= OnSourceChangedAsync;

        await _cancellationTokenSource.CancelAsync();
        _cancellationTokenSource.Dispose();
        await Observer.DisposeAsync();

        if (_scriptModule != null)
            await _scriptModule.DisposeAsync();
    }

    protected async Task TogglePausedAsync()
    {
        ViewState.IsPaused = !ViewState.IsPaused;

        if (!ViewState.IsPaused)
        {
            ViewState.FlushPendingRows();

            if (ViewState.FollowTail && !ViewState.IsPaused)
                RequestScrollToBottomAfterRender();
        }

        await InvokeAsync(StateHasChanged);
    }

    protected Task ClearAsync()
    {
        ViewState.ClearVisibleRows();
        BackendDroppedCount = 0;
        DiscardedRefreshBufferRows = 0;
        return InvokeAsync(StateHasChanged);
    }

    protected async Task ReconnectAsync()
    {
        if (IsLoading)
            return;

        ErrorMessage = null;
        await Observer.ReconnectAsync(ViewState.Filter, _cancellationTokenSource.Token);
    }

    protected async Task CopyVisibleAsync()
    {
        await CopyTextAsync(ExportFormatter.FormatVisibleRows(Rows), $"{Rows.Count} console row(s) copied.");
    }

    protected async Task ExportVisibleAsync()
    {
        var text = ExportFormatter.FormatVisibleRows(Rows);

        if (string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            var module = await GetScriptModuleAsync();
            await module.InvokeVoidAsync("downloadTextFile", CreateExportFileName(DateTimeOffset.Now), text, "text/tab-separated-values;charset=utf-8");
            Snackbar.Add($"{Rows.Count} console row(s) exported.", Severity.Success);
        }
        catch (JSException)
        {
            Snackbar.Add("Unable to export. Check browser download permissions.", Severity.Warning);
        }
    }

    protected async Task SetSourceAsync(string? sourceId)
    {
        if (IsLoading)
            return;

        ViewState.Filter.SourceId = string.IsNullOrWhiteSpace(sourceId) ? null : sourceId;
        await RefreshFilterAsync();
    }

    protected async Task SetStreamAsync(string stream)
    {
        if (IsLoading)
            return;

        ViewState.Filter.Stream = ConsoleLogUrlStateMapper.ParseStream(stream);
        await RefreshFilterAsync();
    }

    protected async Task SetFollowTailAsync(bool value)
    {
        ViewState.FollowTail = value;
        UpdateUrlFromState();

        if (value)
            RequestScrollToBottomAfterRender();

        await InvokeAsync(StateHasChanged);
    }

    protected Task SetWrapAsync(bool value)
    {
        ViewState.Wrap = value;
        UpdateUrlFromState();
        return InvokeAsync(StateHasChanged);
    }

    protected Task SetCompactAsync(bool value)
    {
        ViewState.Compact = value;
        UpdateUrlFromState();
        return InvokeAsync(StateHasChanged);
    }

    protected Task SetAnsiAsync(bool value)
    {
        ViewState.Ansi = value;
        UpdateUrlFromState();
        return InvokeAsync(StateHasChanged);
    }

    protected static string FormatTimestamp(DateTimeOffset timestamp) => timestamp.ToLocalTime().ToString("HH:mm:ss.fff");
    protected static string CreateExportFileName(DateTimeOffset timestamp) => $"diagnostics-console-logs-{timestamp:yyyyMMdd-HHmmss}.tsv";
    protected static string StreamCssClass(ConsoleLogStream stream) => $"console-log-stream console-log-stream-{ConsoleLogExportFormatter.StreamLabel(stream)}";
    protected string RowCssClass(ConsoleLogLine line) => $"console-log-row console-log-row-{ConsoleLogExportFormatter.StreamLabel(line.Stream)}";
    protected RenderFragment RenderDisplayText(ConsoleLogLine line) => builder =>
    {
        if (ViewState.Ansi)
        {
            builder.OpenComponent<AnsiLine>(0);
            builder.AddAttribute(1, nameof(AnsiLine.Segments), GetParsedText(line).Segments);
            builder.CloseComponent();
            return;
        }

        builder.AddContent(0, GetStrippedText(line));
    };
    protected static string Shorten(string? value, int maxLength) => string.IsNullOrWhiteSpace(value) || value.Length <= maxLength ? value ?? "" : string.Concat(value.AsSpan(0, Math.Max(0, maxLength - 1)), "...");
    protected static string SourceDisplayName(ConsoleLogSource source) => ConsoleLogExportFormatter.SourceDisplayName(source);
    protected static bool HasSourceHealthBadge(ConsoleLogSource source) => source.Health is not ConsoleLogSourceHealth.Connected and not ConsoleLogSourceHealth.Unknown;

    protected static string SourceTitle(ConsoleLogSource source)
    {
        var builder = new StringBuilder(source.Id);

        foreach (var value in new[] { source.ServiceName, source.MachineName, source.PodName, source.ContainerName, source.Namespace, source.NodeName })
        {
            if (!string.IsNullOrWhiteSpace(value))
                builder.Append(" | ").Append(value);
        }

        if (source.Health != ConsoleLogSourceHealth.Connected)
            builder.Append(" | ").Append(source.Health);

        return builder.ToString();
    }

    private async Task ActivateAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            await LoadSourcesAsync(cancellationToken);
            var recent = await ConsoleLogService.GetRecentAsync(ViewState.Filter, ViewState.VisibleRowCap, cancellationToken);
            BackendDroppedCount = recent.DroppedLineCount;

            if (recent.Sources is { Count: > 0 })
                MergeSources(recent.Sources);

            foreach (var line in recent.Items.OrderBy(x => x.Timestamp))
                ViewState.AddVisibleLine(line);

            await Observer.StartAsync(ViewState.Filter, cancellationToken);

            if (ViewState.FollowTail && !ViewState.IsPaused)
                RequestScrollToBottomAfterRender();
        }
        catch (Exception e) when (IsAuthorizationFailure(e))
        {
            ErrorMessage = null;
            ViewState.ConnectionStatus = ConsoleLogConnectionStatus.Unauthorized;
            Snackbar.Add(StateMessage ?? "You do not have permission to view diagnostics console logs.", Severity.Error);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            ErrorMessage = e.Message;
            ViewState.ConnectionStatus = ConsoleLogConnectionStatus.Error;
            Snackbar.Add(e.Message, Severity.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshFilterAsync()
    {
        var refreshVersion = Interlocked.Increment(ref _refreshVersion);
        var filter = CopyFilter(ViewState.Filter);
        IsLoading = true;
        _isRefreshing = true;
        _refreshBufferedLines.Clear();
        DiscardedRefreshBufferRows = 0;
        ErrorMessage = null;
        UpdateUrlFromState();

        try
        {
            var recent = await ConsoleLogService.GetRecentAsync(filter, ViewState.VisibleRowCap, _cancellationTokenSource.Token);

            if (!IsCurrentRefresh(refreshVersion))
                return;

            ViewState.ClearVisibleRows();
            BackendDroppedCount = recent.DroppedLineCount;

            if (recent.Sources is { Count: > 0 })
                MergeSources(recent.Sources);

            foreach (var line in recent.Items.OrderBy(x => x.Timestamp))
                ViewState.AddVisibleLine(line);

            if (!IsCurrentRefresh(refreshVersion))
                return;

            await Observer.UpdateFilterAsync(filter, _cancellationTokenSource.Token);

            if (!IsCurrentRefresh(refreshVersion))
                return;

            FlushRefreshBufferedLines();
            _isRefreshing = false;

            if (ViewState.FollowTail && !ViewState.IsPaused)
                RequestScrollToBottomAfterRender();
        }
        catch (Exception e) when (IsAuthorizationFailure(e))
        {
            if (!IsCurrentRefresh(refreshVersion))
                return;

            ErrorMessage = null;
            ViewState.ConnectionStatus = ConsoleLogConnectionStatus.Unauthorized;
            Snackbar.Add(StateMessage ?? "You do not have permission to view diagnostics console logs.", Severity.Error);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            if (!IsCurrentRefresh(refreshVersion))
                return;

            ErrorMessage = e.Message;
            ViewState.ConnectionStatus = ConsoleLogConnectionStatus.Error;
            Snackbar.Add(e.Message, Severity.Error);
        }
        finally
        {
            if (IsCurrentRefresh(refreshVersion))
            {
                _isRefreshing = false;
                _refreshBufferedLines.Clear();
                IsLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task LoadSourcesAsync(CancellationToken cancellationToken)
    {
        var sources = await ConsoleLogService.ListSourcesAsync(cancellationToken);
        _sources.Clear();
        MergeSources(sources);
    }

    private Task OnLineReceivedAsync(ConsoleLogLine line)
    {
        return InvokeAsync(async () =>
        {
            if (!ConsoleLogFilterMatcher.IsMatch(line, ViewState.Filter))
                return;

            if (_isRefreshing)
            {
                var discardedRows = DiscardedRefreshBufferRows;
                AddRefreshBufferedLine(line);

                if (DiscardedRefreshBufferRows != discardedRows)
                    StateHasChanged();

                return;
            }

            ViewState.AddIncomingLine(line);

            if (!_isRefreshing && !ViewState.IsPaused && ViewState.FollowTail)
                RequestScrollToBottomAfterRender();

            StateHasChanged();
        });
    }

    private Task OnDroppedLinesReceivedAsync(ConsoleLogDroppedLineSummary summary)
    {
        return InvokeAsync(() =>
        {
            BackendDroppedCount += summary.Count;
            StateHasChanged();
        });
    }

    private Task OnConnectionStatusChangedAsync(ConsoleLogConnectionStatus status)
    {
        return InvokeAsync(() =>
        {
            ViewState.ConnectionStatus = status;
            StateHasChanged();
        });
    }

    private Task OnSourceChangedAsync(ConsoleLogSource source)
    {
        return InvokeAsync(() =>
        {
            MergeSources([source]);
            StateHasChanged();
        });
    }

    private void MergeSources(IEnumerable<ConsoleLogSource> sources)
    {
        foreach (var source in sources)
        {
            var index = _sources.FindIndex(x => string.Equals(x.Id, source.Id, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
                _sources[index] = source;
            else
                _sources.Add(source);
        }

        _sources.Sort((left, right) => string.Compare(SourceDisplayName(left), SourceDisplayName(right), StringComparison.OrdinalIgnoreCase));
    }

    private void ApplyQueryFromUrl()
    {
        if (_queryInitialized)
            return;

        UrlStateMapper.ApplyQuery(ViewState, NavigationManager.ToAbsoluteUri(NavigationManager.Uri));
        _queryInitialized = true;
    }

    private void UpdateUrlFromState()
    {
        if (!UseUrlState || !_queryInitialized)
            return;

        var uri = NavigationManager.GetUriWithQueryParameters(UrlStateMapper.ToQueryParameters(ViewState));

        if (!string.Equals(uri, NavigationManager.Uri, StringComparison.Ordinal))
            NavigationManager.NavigateTo(uri, replace: true);
    }

    private async Task CopyTextAsync(string text, string successMessage)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
            Snackbar.Add(successMessage, Severity.Success);
        }
        catch (JSException)
        {
            Snackbar.Add("Unable to copy. Check browser clipboard permissions.", Severity.Warning);
        }
    }

    private async Task ScrollToBottomAsync()
    {
        try
        {
            var module = await GetScriptModuleAsync();
            await module.InvokeVoidAsync("scrollToBottomById", LogSurfaceId);
        }
        catch (JSException)
        {
            // Scrolling is best-effort and should not interrupt console streaming.
        }
    }

    private void RequestScrollToBottomAfterRender()
    {
        _scrollAfterRender = true;
    }

    private async Task<IJSObjectReference> GetScriptModuleAsync()
    {
        _scriptModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Elsa.Studio.Diagnostics.ConsoleLogs/consoleLogs.js");
        return _scriptModule;
    }

    protected string GetStrippedText(ConsoleLogLine line)
    {
        return GetParsedText(line).StrippedText;
    }

    private ParsedConsoleLogText GetParsedText(ConsoleLogLine line)
    {
        if (!_parsedTextCache.TryGetValue(line, out var cachedText))
        {
            var segments = AnsiSgrParser.Parse(line.Text);
            cachedText = new ParsedConsoleLogText(segments, StripAnsi(segments));
            _parsedTextCache.Add(line, cachedText);
        }

        return cachedText;
    }

    private void AddRefreshBufferedLine(ConsoleLogLine line)
    {
        _refreshBufferedLines.Enqueue(line);

        while (_refreshBufferedLines.Count > ViewState.VisibleRowCap)
        {
            _refreshBufferedLines.Dequeue();
            DiscardedRefreshBufferRows++;
        }
    }

    private void FlushRefreshBufferedLines()
    {
        if (_refreshBufferedLines.Count == 0)
            return;

        var visibleIds = Rows
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .Select(x => x.Id)
            .ToHashSet(StringComparer.Ordinal);

        while (_refreshBufferedLines.TryDequeue(out var line))
        {
            if (!string.IsNullOrWhiteSpace(line.Id) && !visibleIds.Add(line.Id))
                continue;

            ViewState.AddIncomingLine(line);
        }
    }

    private bool IsCurrentRefresh(long refreshVersion) => Interlocked.Read(ref _refreshVersion) == refreshVersion;

    private static bool IsAuthorizationFailure(Exception e)
    {
        return e switch
        {
            ApiException { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden } => true,
            HttpRequestException { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden } => true,
            _ => false
        };
    }

    private static ConsoleLogFilter CopyFilter(ConsoleLogFilter filter) => new()
    {
        SourceId = filter.SourceId,
        Stream = filter.Stream,
        Query = filter.Query,
        WorkflowInstanceId = filter.WorkflowInstanceId,
        ActivityInstanceId = filter.ActivityInstanceId,
        ActivityId = filter.ActivityId,
        ActivityNodeId = filter.ActivityNodeId,
        From = filter.From,
        To = filter.To,
        Limit = filter.Limit
    };

    private sealed class ParsedConsoleLogText(IReadOnlyList<AnsiSegment> segments, string strippedText)
    {
        public IReadOnlyList<AnsiSegment> Segments { get; } = segments;
        public string StrippedText { get; } = strippedText;
    }

    private void ApplyScopeParameters()
    {
        _appliedWorkflowInstanceId = NormalizeScopeValue(WorkflowInstanceId);
        _appliedActivityInstanceId = NormalizeScopeValue(ActivityInstanceId);
        _appliedActivityId = NormalizeScopeValue(ActivityId);
        _appliedActivityNodeId = NormalizeScopeValue(ActivityNodeId);
        ViewState.Filter.WorkflowInstanceId = _appliedWorkflowInstanceId;
        ViewState.Filter.ActivityInstanceId = _appliedActivityInstanceId;
        ViewState.Filter.ActivityId = _appliedActivityId;
        ViewState.Filter.ActivityNodeId = _appliedActivityNodeId;
    }

    private static string? NormalizeScopeValue(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    protected static string StripAnsi(string text) => StripAnsi(AnsiSgrParser.Parse(text));

    private static string StripAnsi(IEnumerable<AnsiSegment> segments) => string.Concat(segments.Select(segment => segment.Text));
}
