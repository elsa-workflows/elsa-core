using Elsa.Studio.Diagnostics.StructuredLogs.Contracts;
using Elsa.Studio.Diagnostics.StructuredLogs.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using MudBlazor;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Refit;

#pragma warning disable IL2026 // Raw details copy mirrors existing Studio JSON display behavior.

namespace Elsa.Studio.Diagnostics.StructuredLogs.UI.Pages;

/// <summary>
/// Structured logs page.
/// </summary>
[UsedImplicitly]
public partial class StructuredLogs : IAsyncDisposable
{
    private const string LogSurfaceId = "structured-logs-surface";
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly IReadOnlyCollection<StructuredLogLevel> LogLevels =
    [
        StructuredLogLevel.Trace,
        StructuredLogLevel.Debug,
        StructuredLogLevel.Information,
        StructuredLogLevel.Warning,
        StructuredLogLevel.Error,
        StructuredLogLevel.Critical
    ];

    private readonly List<StructuredLogEvent> _rows = new();
    private readonly List<StructuredLogSource> _sources = new();
    private readonly HashSet<string> _selectedLogIds = new(StringComparer.Ordinal);
    private CancellationTokenSource _cancellationTokenSource = new();
    private IJSObjectReference? _scriptModule;
    private string? _fromFilterText;
    private string? _toFilterText;
    private bool _queryInitialized;
    private bool _terminalFailure;
    private bool _observerStopped;
    private bool _isDisposed;
    private bool _hasRenderedInteractively;
    private bool _scrollPending;

    /// <summary>
    /// Gets or sets the structured log service.
    /// </summary>
    [Inject] private IStructuredLogService StructuredLogService { get; set; } = default!;

    /// <summary>
    /// Gets or sets the live observer.
    /// </summary>
    [Inject] private IStructuredLogObserver Observer { get; set; } = default!;

    /// <summary>
    /// Gets or sets the snackbar service.
    /// </summary>
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    /// <summary>
    /// Gets or sets the JS runtime.
    /// </summary>
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Gets or sets the navigation manager.
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>
    /// Gets the current view state.
    /// </summary>
    protected StructuredLogViewState ViewState { get; } = new();

    /// <summary>
    /// Gets the rendered rows.
    /// </summary>
    protected IReadOnlyList<StructuredLogEvent> Rows => _rows;

    /// <summary>
    /// Gets the available backend log sources.
    /// </summary>
    protected IReadOnlyList<StructuredLogSource> Sources => _sources;
    protected StructuredLogEvent? SelectedLogEvent => _rows.FirstOrDefault(x => string.Equals(x.Id, ViewState.SelectedEventId, StringComparison.Ordinal));
    protected string SelectedRawJson => SelectedLogEvent == null ? "" : JsonSerializer.Serialize(SelectedLogEvent, JsonSerializerOptions);
    protected string? FromFilterText => _fromFilterText;
    protected string? ToFilterText => _toFilterText;

    /// <summary>
    /// Gets a value indicating whether the page is loading.
    /// </summary>
    protected bool IsLoading { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the diagnostics surface is unavailable, access was denied, or the component was disposed.
    /// </summary>
    protected bool IsTerminalFailure => _isDisposed || _terminalFailure || ViewState.ConnectionStatus is StructuredLogConnectionStatus.Unauthorized or StructuredLogConnectionStatus.Unavailable;

    /// <summary>
    /// Gets the last error message.
    /// </summary>
    protected string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the upstream dropped event count.
    /// </summary>
    protected long BackendDroppedCount { get; private set; }

    /// <summary>
    /// Gets the storage-level dropped write count.
    /// </summary>
    protected long StorageDroppedWriteCount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the backend has a storage diagnostics provider.
    /// </summary>
    protected bool HasStorageDiagnosticsProvider { get; private set; }

    protected string StatusText => ViewState.ConnectionStatus switch
    {
        StructuredLogConnectionStatus.Connecting => "Connecting",
        StructuredLogConnectionStatus.Connected => "Connected",
        StructuredLogConnectionStatus.Reconnecting => "Reconnecting",
        StructuredLogConnectionStatus.Unavailable => "Unavailable",
        StructuredLogConnectionStatus.Unauthorized => "Unauthorized",
        _ => "Disconnected"
    };

    protected string StatusCssClass => $"structured-logs-status-{ViewState.ConnectionStatus.ToString().ToLowerInvariant()}";
    protected string PauseIcon => ViewState.IsPaused ? Icons.Material.Filled.PlayArrow : Icons.Material.Filled.Pause;
    protected string PauseTooltip => ViewState.IsPaused ? "Resume" : "Pause";
    protected string MainCssClass => $"structured-logs-main{(SelectedLogEvent == null ? " structured-logs-main-no-details" : "")}";
    protected string LogSurfaceCssClass => $"structured-logs-surface{(ViewState.WrapMessages ? " structured-logs-wrap" : "")}{(ViewState.Compact ? " structured-logs-compact" : "")}";
    protected bool HasSelection => _selectedLogIds.Count > 0;
    protected bool HasActiveFilter => !string.IsNullOrWhiteSpace(ViewState.Filter.SourceId) ||
                                      ViewState.Filter.MinimumLevel is { } level && level != StructuredLogLevel.Information ||
                                      !string.IsNullOrWhiteSpace(ViewState.Filter.Text) ||
                                      !string.IsNullOrWhiteSpace(ViewState.Filter.CategoryPrefix) ||
                                      !string.IsNullOrWhiteSpace(ViewState.Filter.TenantId) ||
                                      !string.IsNullOrWhiteSpace(ViewState.Filter.WorkflowDefinitionId) ||
                                      !string.IsNullOrWhiteSpace(ViewState.Filter.WorkflowInstanceId) ||
                                      !string.IsNullOrWhiteSpace(ViewState.Filter.TraceId) ||
                                      !string.IsNullOrWhiteSpace(ViewState.Filter.SpanId) ||
                                      !string.IsNullOrWhiteSpace(ViewState.Filter.CorrelationId) ||
                                      ViewState.Filter.From != null ||
                                      ViewState.Filter.To != null;
    protected string EmptyText => HasActiveFilter ? "No structured logs match the current filters." : "No structured logs received yet.";
    protected string? StateMessage => ViewState.ConnectionStatus switch
    {
        StructuredLogConnectionStatus.Unauthorized => "You do not have permission to view structured logs.",
        StructuredLogConnectionStatus.Unavailable => "Diagnostics structured logs are not available on this backend.",
        StructuredLogConnectionStatus.Disconnected when Rows.Count == 0 => "The live structured log subscription is disconnected.",
        StructuredLogConnectionStatus.Reconnecting => "Reconnecting to the live structured log subscription.",
        _ => null
    };

    protected Severity StateSeverity => ViewState.ConnectionStatus switch
    {
        StructuredLogConnectionStatus.Unauthorized => Severity.Error,
        StructuredLogConnectionStatus.Unavailable => Severity.Warning,
        StructuredLogConnectionStatus.Disconnected => Severity.Warning,
        _ => Severity.Info
    };

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        ApplyQueryFromUrl();
        Observer.LogReceived += OnLogReceivedAsync;
        Observer.DroppedEventsReceived += OnDroppedEventsReceivedAsync;
        Observer.ConnectionStatusChanged += OnConnectionStatusChangedAsync;
        Observer.SourceChanged += OnSourceChangedAsync;
        await ActivateAsync(_cancellationTokenSource.Token);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        _hasRenderedInteractively = true;

        if (!_scrollPending)
            return;

        _scrollPending = false;
        if (IsTerminalFailure || _isDisposed)
            return;

        await ScrollToBottomAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _isDisposed = true;
        await StopObserverAsync();

        await _cancellationTokenSource.CancelAsync();
        _cancellationTokenSource.Dispose();

        if (_scriptModule != null)
            await _scriptModule.DisposeAsync();
    }

    protected async Task TogglePausedAsync()
    {
        ViewState.IsPaused = !ViewState.IsPaused;

        if (!ViewState.IsPaused)
            await RequestScrollToBottomAsync();
    }

    protected Task ClearAsync()
    {
        ClearBufferedData();
        return InvokeAsync(StateHasChanged);
    }

    protected async Task ReconnectAsync()
    {
        if (IsTerminalFailure)
            return;

        ErrorMessage = null;
        await Observer.ReconnectAsync(ViewState.Filter, _cancellationTokenSource.Token);
    }

    protected async Task SetSourceAsync(string? sourceId)
    {
        ViewState.Filter.SourceId = string.IsNullOrWhiteSpace(sourceId) ? null : sourceId;
        await RefreshFilterAsync();
    }

    protected async Task SetMinimumLevelAsync(StructuredLogLevel? level)
    {
        ViewState.Filter.MinimumLevel = level;
        await RefreshFilterAsync();
    }

    protected async Task SetTextFilterAsync(string? text)
    {
        ViewState.Filter.Text = string.IsNullOrWhiteSpace(text) ? null : text;
        await RefreshFilterAsync();
    }

    protected async Task SetCategoryPrefixAsync(string? categoryPrefix)
    {
        ViewState.Filter.CategoryPrefix = NormalizeFilterText(categoryPrefix);
        await RefreshFilterAsync();
    }

    protected async Task SetTenantIdAsync(string? tenantId)
    {
        ViewState.Filter.TenantId = NormalizeFilterText(tenantId);
        await RefreshFilterAsync();
    }

    protected async Task SetWorkflowDefinitionIdAsync(string? workflowDefinitionId)
    {
        ViewState.Filter.WorkflowDefinitionId = NormalizeFilterText(workflowDefinitionId);
        await RefreshFilterAsync();
    }

    protected async Task SetWorkflowInstanceIdAsync(string? workflowInstanceId)
    {
        ViewState.Filter.WorkflowInstanceId = NormalizeFilterText(workflowInstanceId);
        await RefreshFilterAsync();
    }

    protected async Task SetTraceIdAsync(string? traceId)
    {
        ViewState.Filter.TraceId = NormalizeFilterText(traceId);
        await RefreshFilterAsync();
    }

    protected async Task SetSpanIdAsync(string? spanId)
    {
        ViewState.Filter.SpanId = NormalizeFilterText(spanId);
        await RefreshFilterAsync();
    }

    protected async Task SetCorrelationIdAsync(string? correlationId)
    {
        ViewState.Filter.CorrelationId = NormalizeFilterText(correlationId);
        await RefreshFilterAsync();
    }

    protected async Task SetFromAsync(string? value)
    {
        _fromFilterText = NormalizeFilterText(value);
        ViewState.Filter.From = TryParseDateTimeOffset(_fromFilterText, out var parsed) ? parsed : null;
        await RefreshFilterAsync();
    }

    protected async Task SetToAsync(string? value)
    {
        _toFilterText = NormalizeFilterText(value);
        ViewState.Filter.To = TryParseDateTimeOffset(_toFilterText, out var parsed) ? parsed : null;
        await RefreshFilterAsync();
    }

    protected async Task SetAutoScrollAsync(bool value)
    {
        ViewState.AutoScroll = value;

        if (value)
            await RequestScrollToBottomAsync();
    }

    protected Task SetWrapMessagesAsync(bool value)
    {
        ViewState.WrapMessages = value;
        return InvokeAsync(StateHasChanged);
    }

    protected Task SetCompactAsync(bool value)
    {
        ViewState.Compact = value;
        return InvokeAsync(StateHasChanged);
    }

    protected static string FormatTimestamp(DateTimeOffset timestamp) => timestamp.ToLocalTime().ToString("HH:mm:ss.fff");

    protected static string LevelLabel(StructuredLogLevel level) => level switch
    {
        StructuredLogLevel.Trace => "TRC",
        StructuredLogLevel.Debug => "DBG",
        StructuredLogLevel.Information => "INF",
        StructuredLogLevel.Warning => "WRN",
        StructuredLogLevel.Error => "ERR",
        StructuredLogLevel.Critical => "CRT",
        _ => level.ToString()
    };

    protected static string Shorten(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            return value ?? "";

        return string.Concat(value.AsSpan(0, Math.Max(0, maxLength - 1)), "...");
    }

    protected static string LevelCssClass(StructuredLogLevel level) => $"structured-log-level structured-log-level-{level.ToString().ToLowerInvariant()}";

    protected string RowCssClass(StructuredLogEvent logEvent) => $"structured-log-row structured-log-row-{logEvent.Level.ToString().ToLowerInvariant()}{(IsSelected(logEvent) ? " structured-log-row-selected" : "")}";

    protected string RowSelectionLabel(StructuredLogEvent logEvent) => $"Select log row {FormatTimestamp(logEvent.Timestamp)} {logEvent.Level} {Shorten(logEvent.Category, 64)}";

    protected bool IsSelected(StructuredLogEvent logEvent) => _selectedLogIds.Contains(logEvent.Id);

    protected Task SetSelectedAsync(StructuredLogEvent logEvent, bool selected)
    {
        if (selected)
            _selectedLogIds.Add(logEvent.Id);
        else
            _selectedLogIds.Remove(logEvent.Id);

        return InvokeAsync(StateHasChanged);
    }

    protected async Task CopySelectedAsync()
    {
        var selectedRows = _rows.Where(x => _selectedLogIds.Contains(x.Id)).ToList();
        await CopyRowsAsync(selectedRows);
    }

    protected Task CopyVisibleAsync()
    {
        return CopyRowsAsync(_rows);
    }

    protected Task SelectDetailsAsync(StructuredLogEvent logEvent)
    {
        ViewState.SelectedEventId = logEvent.Id;
        return InvokeAsync(StateHasChanged);
    }

    protected Task CloseDetailsAsync()
    {
        ViewState.SelectedEventId = null;
        return InvokeAsync(StateHasChanged);
    }

    protected Task CopyDetailsAsync()
    {
        return CopyTextAsync(SelectedRawJson, "Structured log details copied.");
    }

    protected Task CopyValueAsync(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? Task.CompletedTask : CopyTextAsync(value, "Value copied.");
    }

    protected string SourceDisplayName(string? sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return "";

        var source = _sources.FirstOrDefault(x => string.Equals(x.Id, sourceId, StringComparison.OrdinalIgnoreCase));
        return source == null ? sourceId : SourceDisplayName(source);
    }

    protected static string SourceDisplayName(StructuredLogSource source)
    {
        var name = !string.IsNullOrWhiteSpace(source.DisplayName) ? source.DisplayName : source.Id;
        return source.Status == StructuredLogSourceStatus.Connected ? name : $"{name} ({source.Status})";
    }

    protected static string FormatValue(object? value)
    {
        return value switch
        {
            null => "",
            string text => text,
            JsonElement jsonElement => jsonElement.ValueKind == JsonValueKind.String ? jsonElement.GetString() ?? "" : jsonElement.GetRawText(),
            _ => JsonSerializer.Serialize(value, JsonSerializerOptions)
        };
    }

    protected static string CorrelationHint(StructuredLogEvent logEvent)
    {
        if (!string.IsNullOrWhiteSpace(logEvent.TraceId))
            return Shorten($"trace:{logEvent.TraceId}", 28);

        if (!string.IsNullOrWhiteSpace(logEvent.CorrelationId))
            return Shorten($"corr:{logEvent.CorrelationId}", 28);

        return "";
    }

    protected static bool HasCorrelationHint(StructuredLogEvent logEvent) => !string.IsNullOrWhiteSpace(logEvent.TraceId) || !string.IsNullOrWhiteSpace(logEvent.CorrelationId);

    protected static string CorrelationFilterTooltip(StructuredLogEvent logEvent)
    {
        if (!string.IsNullOrWhiteSpace(logEvent.TraceId))
            return $"Filter by trace ID {logEvent.TraceId}";

        if (!string.IsNullOrWhiteSpace(logEvent.CorrelationId))
            return $"Filter by correlation ID {logEvent.CorrelationId}";

        return "";
    }

    protected Task ApplyCorrelationFilterAsync(StructuredLogEvent logEvent)
    {
        if (!string.IsNullOrWhiteSpace(logEvent.TraceId))
            return SetTraceIdAsync(logEvent.TraceId);

        return SetCorrelationIdAsync(logEvent.CorrelationId);
    }

    protected RenderFragment DetailRow(string label, string? value, Func<string?, Task>? applyFilterAsync = null, string? href = null, string? hrefLabel = null) => builder =>
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        builder.OpenElement(0, "dt");
        builder.AddContent(1, Localizer[label]);
        builder.CloseElement();

        builder.OpenElement(2, "dd");
        builder.OpenElement(3, "code");
        builder.AddContent(4, value);
        builder.CloseElement();
        builder.OpenElement(5, "button");
        builder.AddAttribute(6, "type", "button");
        builder.AddAttribute(7, "class", "structured-log-detail-action");
        builder.AddAttribute(8, "title", Localizer["Copy"]);
        builder.AddAttribute(9, "onclick", EventCallback.Factory.Create(this, () => CopyValueAsync(value)));
        builder.AddContent(10, "Copy");
        builder.CloseElement();

        if (applyFilterAsync != null)
        {
            builder.OpenElement(11, "button");
            builder.AddAttribute(12, "type", "button");
            builder.AddAttribute(13, "class", "structured-log-detail-action");
            builder.AddAttribute(14, "title", Localizer["Filter"]);
            builder.AddAttribute(15, "onclick", EventCallback.Factory.Create(this, () => applyFilterAsync(value)));
            builder.AddContent(16, "Filter");
            builder.CloseElement();
        }

        if (!string.IsNullOrWhiteSpace(href))
        {
            builder.OpenElement(17, "a");
            builder.AddAttribute(18, "class", "structured-log-detail-action");
            builder.AddAttribute(19, "href", href);
            builder.AddContent(20, hrefLabel ?? "Open");
            builder.CloseElement();
        }

        builder.CloseElement();
    };

    protected string? OpenTelemetryHref(StructuredLogEvent logEvent, bool includeSpan)
    {
        if (string.IsNullOrWhiteSpace(logEvent.TraceId))
            return null;

        return NavigationManager.GetUriWithQueryParameters("diagnostics/opentelemetry", new Dictionary<string, object?>
        {
            ["tab"] = "Traces",
            ["trace"] = logEvent.TraceId,
            ["span"] = includeSpan ? logEvent.SpanId : null,
            ["workflow"] = logEvent.WorkflowInstanceId,
            ["definition"] = logEvent.WorkflowDefinitionId
        });
    }

    private async Task ActivateAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            await LoadSourcesAsync(cancellationToken);
            if (IsTerminalFailure)
                return;

            var recent = await StructuredLogService.GetRecentAsync(ViewState.Filter, ViewState.VisibleRowCap, cancellationToken);
            if (IsTerminalFailure)
                return;

            var storageDiagnostics = await StructuredLogService.GetStorageDiagnosticsAsync(cancellationToken);
            if (IsTerminalFailure)
                return;

            BackendDroppedCount = recent.DroppedEvents;
            StorageDroppedWriteCount = storageDiagnostics.DroppedWriteCount;
            HasStorageDiagnosticsProvider = storageDiagnostics.HasStorageDiagnosticsProvider;

            foreach (var logEvent in recent.Items.OrderBy(x => x.Timestamp))
                AddRow(logEvent);

            await Observer.StartAsync(ViewState.Filter, cancellationToken);
            if (IsTerminalFailure)
                return;

            await RequestScrollToBottomAsync();
        }
        catch (Exception e) when (IsAuthorizationFailure(e))
        {
            await EnterTerminalFailureAsync(StructuredLogConnectionStatus.Unauthorized);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            ErrorMessage = e.Message;
            Snackbar.Add(e.Message, Severity.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshFilterAsync()
    {
        if (IsTerminalFailure)
            return;

        IsLoading = true;
        ErrorMessage = null;
        UpdateUrlFromFilter();

        try
        {
            _rows.Clear();
            _selectedLogIds.Clear();
            ViewState.SelectedEventId = null;
            ViewState.LocalDroppedRows = 0;
            BackendDroppedCount = 0;
            StorageDroppedWriteCount = 0;
            HasStorageDiagnosticsProvider = false;

            var recent = await StructuredLogService.GetRecentAsync(ViewState.Filter, ViewState.VisibleRowCap, _cancellationTokenSource.Token);
            if (IsTerminalFailure)
                return;

            var storageDiagnostics = await StructuredLogService.GetStorageDiagnosticsAsync(_cancellationTokenSource.Token);
            if (IsTerminalFailure)
                return;

            BackendDroppedCount = recent.DroppedEvents;
            StorageDroppedWriteCount = storageDiagnostics.DroppedWriteCount;
            HasStorageDiagnosticsProvider = storageDiagnostics.HasStorageDiagnosticsProvider;

            foreach (var logEvent in recent.Items.OrderBy(x => x.Timestamp))
                AddRow(logEvent);

            await Observer.UpdateFilterAsync(ViewState.Filter, _cancellationTokenSource.Token);
            if (IsTerminalFailure)
                return;

            await RequestScrollToBottomAsync();
        }
        catch (Exception e) when (IsAuthorizationFailure(e))
        {
            await EnterTerminalFailureAsync(StructuredLogConnectionStatus.Unauthorized);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            ErrorMessage = e.Message;
            Snackbar.Add(e.Message, Severity.Error);
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadSourcesAsync(CancellationToken cancellationToken)
    {
        var sources = await StructuredLogService.ListSourcesAsync(cancellationToken);
        _sources.Clear();
        _sources.AddRange(sources.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase));
    }

    private async Task OnLogReceivedAsync(StructuredLogEvent logEvent)
    {
        await InvokeAsync(async () =>
        {
            if (_terminalFailure || _isDisposed || ViewState.IsPaused)
                return;

            AddRow(logEvent);
            StateHasChanged();

            if (ViewState.AutoScroll)
                await RequestScrollToBottomAsync();
        });
    }

    private Task OnDroppedEventsReceivedAsync(StructuredLogDroppedEventSummary summary)
    {
        return InvokeAsync(() =>
        {
            if (_terminalFailure || _isDisposed)
                return;

            BackendDroppedCount += summary.DroppedCount;
            StateHasChanged();
        });
    }

    private Task OnConnectionStatusChangedAsync(StructuredLogConnectionStatus status)
    {
        return InvokeAsync(async () =>
        {
            if (_terminalFailure || _isDisposed)
                return;

            if (status is StructuredLogConnectionStatus.Unauthorized or StructuredLogConnectionStatus.Unavailable)
            {
                await EnterTerminalFailureAsync(status);
                return;
            }

            ViewState.ConnectionStatus = status;
            StateHasChanged();
        });
    }

    private Task OnSourceChangedAsync(StructuredLogSource source)
    {
        return InvokeAsync(() =>
        {
            if (_terminalFailure || _isDisposed)
                return;

            var index = _sources.FindIndex(x => string.Equals(x.Id, source.Id, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
                _sources[index] = source;
            else
                _sources.Add(source);

            _sources.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
            StateHasChanged();
        });
    }

    private async Task EnterTerminalFailureAsync(StructuredLogConnectionStatus status)
    {
        if (_terminalFailure || _isDisposed)
            return;

        _terminalFailure = true;
        _scrollPending = false;
        IsLoading = false;
        ErrorMessage = null;
        ViewState.ConnectionStatus = status;
        _sources.Clear();
        ClearBufferedData();
        await _cancellationTokenSource.CancelAsync();
        await StopObserverAsync();
        await InvokeAsync(StateHasChanged);
    }

    private void ClearBufferedData()
    {
        _rows.Clear();
        _selectedLogIds.Clear();
        ViewState.SelectedEventId = null;
        ViewState.LocalDroppedRows = 0;
        BackendDroppedCount = 0;
        StorageDroppedWriteCount = 0;
        HasStorageDiagnosticsProvider = false;
    }

    private async Task StopObserverAsync()
    {
        if (_observerStopped)
            return;

        _observerStopped = true;
        Observer.LogReceived -= OnLogReceivedAsync;
        Observer.DroppedEventsReceived -= OnDroppedEventsReceivedAsync;
        Observer.ConnectionStatusChanged -= OnConnectionStatusChangedAsync;
        Observer.SourceChanged -= OnSourceChangedAsync;
        await Observer.DisposeAsync();
    }

    private static bool IsAuthorizationFailure(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current is ApiException { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden } ||
                current is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden })
                return true;
        }

        return false;
    }

    private void AddRow(StructuredLogEvent logEvent)
    {
        _rows.Add(logEvent);

        while (_rows.Count > ViewState.VisibleRowCap)
        {
            if (string.Equals(ViewState.SelectedEventId, _rows[0].Id, StringComparison.Ordinal))
                ViewState.SelectedEventId = null;

            _selectedLogIds.Remove(_rows[0].Id);
            _rows.RemoveAt(0);
            ViewState.LocalDroppedRows++;
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
            // Scrolling is best-effort and should not interrupt structured log updates.
        }
    }

    private Task RequestScrollToBottomAsync()
    {
        if (!_hasRenderedInteractively)
        {
            _scrollPending = true;
            return Task.CompletedTask;
        }

        return ScrollToBottomAsync();
    }

    private void ApplyQueryFromUrl()
    {
        if (_queryInitialized)
            return;

        var query = QueryHelpers.ParseQuery(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query);

        if (query.TryGetValue("sourceId", out var sourceId))
            ViewState.Filter.SourceId = string.IsNullOrWhiteSpace(sourceId) ? null : sourceId.ToString();

        if (query.TryGetValue("level", out var level) && Enum.TryParse<StructuredLogLevel>(level, true, out var parsedLevel))
            ViewState.Filter.MinimumLevel = parsedLevel;

        if (query.TryGetValue("text", out var text))
            ViewState.Filter.Text = string.IsNullOrWhiteSpace(text) ? null : text.ToString();

        if (query.TryGetValue("categoryPrefix", out var categoryPrefix))
            ViewState.Filter.CategoryPrefix = NormalizeFilterText(categoryPrefix);

        if (query.TryGetValue("tenantId", out var tenantId))
            ViewState.Filter.TenantId = NormalizeFilterText(tenantId);

        if (query.TryGetValue("workflowDefinitionId", out var workflowDefinitionId))
            ViewState.Filter.WorkflowDefinitionId = NormalizeFilterText(workflowDefinitionId);

        if (query.TryGetValue("workflowInstanceId", out var workflowInstanceId))
            ViewState.Filter.WorkflowInstanceId = NormalizeFilterText(workflowInstanceId);

        if (query.TryGetValue("traceId", out var traceId))
            ViewState.Filter.TraceId = NormalizeFilterText(traceId);

        if (query.TryGetValue("spanId", out var spanId))
            ViewState.Filter.SpanId = NormalizeFilterText(spanId);

        if (query.TryGetValue("correlationId", out var correlationId))
            ViewState.Filter.CorrelationId = NormalizeFilterText(correlationId);

        if (query.TryGetValue("levels", out var levels))
            ViewState.Filter.Levels = ParseLevels(levels.ToString());

        if (query.TryGetValue("from", out var from))
        {
            _fromFilterText = NormalizeFilterText(from);
            if (TryParseDateTimeOffset(_fromFilterText, out var parsedFrom))
                ViewState.Filter.From = parsedFrom;
        }

        if (query.TryGetValue("to", out var to))
        {
            _toFilterText = NormalizeFilterText(to);
            if (TryParseDateTimeOffset(_toFilterText, out var parsedTo))
                ViewState.Filter.To = parsedTo;
        }

        _queryInitialized = true;
    }

    private void UpdateUrlFromFilter()
    {
        if (!_queryInitialized)
            return;

        var parameters = new Dictionary<string, object?>
        {
            ["sourceId"] = ViewState.Filter.SourceId,
            ["level"] = ViewState.Filter.MinimumLevel?.ToString(),
            ["levels"] = ViewState.Filter.Levels == null ? null : string.Join(",", ViewState.Filter.Levels),
            ["text"] = ViewState.Filter.Text,
            ["categoryPrefix"] = ViewState.Filter.CategoryPrefix,
            ["tenantId"] = ViewState.Filter.TenantId,
            ["workflowDefinitionId"] = ViewState.Filter.WorkflowDefinitionId,
            ["workflowInstanceId"] = ViewState.Filter.WorkflowInstanceId,
            ["traceId"] = ViewState.Filter.TraceId,
            ["spanId"] = ViewState.Filter.SpanId,
            ["correlationId"] = ViewState.Filter.CorrelationId,
            ["from"] = ViewState.Filter.From?.ToString("O"),
            ["to"] = ViewState.Filter.To?.ToString("O")
        };
        var uri = NavigationManager.GetUriWithQueryParameters(parameters);

        if (!string.Equals(uri, NavigationManager.Uri, StringComparison.Ordinal))
            NavigationManager.NavigateTo(uri, replace: true);
    }

    private async Task CopyRowsAsync(IReadOnlyCollection<StructuredLogEvent> rows)
    {
        if (rows.Count == 0)
            return;

        var text = string.Join(Environment.NewLine, rows.Select(FormatCopyLine));

        await CopyTextAsync(text, $"{rows.Count} structured log row(s) copied.");
    }

    private string FormatCopyLine(StructuredLogEvent logEvent)
    {
        return $"{logEvent.Timestamp:O}\t{logEvent.Level}\t{SourceDisplayName(logEvent.SourceId)}\t{logEvent.Category}\t{logEvent.TraceId}\t{logEvent.SpanId}\t{logEvent.Message}";
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

    private static string? NormalizeFilterText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool TryParseDateTimeOffset(string? value, out DateTimeOffset parsed)
    {
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed);
    }

    private static ICollection<StructuredLogLevel>? ParseLevels(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var levels = value
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(x => Enum.TryParse<StructuredLogLevel>(x, true, out var level) ? level : (StructuredLogLevel?)null)
            .OfType<StructuredLogLevel>()
            .Distinct()
            .ToList();

        return levels.Count == 0 ? null : levels;
    }

    private async Task<IJSObjectReference> GetScriptModuleAsync()
    {
        _scriptModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Elsa.Studio.Diagnostics.StructuredLogs/structuredLogs.js");
        return _scriptModule;
    }
}
