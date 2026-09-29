using System.Net;
using System.Net.Http;
using Bunit;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.Diagnostics.StructuredLogs.Contracts;
using Elsa.Studio.Diagnostics.StructuredLogs.Models;
using Elsa.Studio.Diagnostics.StructuredLogs.Services;
using Elsa.Studio.Diagnostics.StructuredLogs.UI.Pages;
using Elsa.Studio.Localization;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;
using Refit;
using Xunit;

namespace Elsa.Studio.Diagnostics.StructuredLogs.Tests;

public sealed class StructuredLogsAuthorizationTests : BunitContext, IAsyncLifetime
{
    private const string JsModule = "./_content/Elsa.Studio.Diagnostics.StructuredLogs/structuredLogs.js";

    public StructuredLogsAuthorizationTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        JSInterop.SetupModule(JsModule).SetupVoid("scrollToBottomById", _ => true).SetVoidResult();
    }

    [Theory]
    [InlineData("rest", 401)]
    [InlineData("rest", 403)]
    [InlineData("signalr", 401)]
    [InlineData("signalr", 403)]
    public async Task Authorization_denial_hides_and_clears_buffered_logs_and_ignores_late_events(string source, int statusCode)
    {
        var service = new TestStructuredLogService(denyRefresh: source == "rest", (HttpStatusCode)statusCode);
        var observer = new TestStructuredLogObserver();
        Services.AddSingleton<IStructuredLogService>(service);
        Services.AddSingleton<IStructuredLogObserver>(observer);
        Render<MudPopoverProvider>();

        var cut = Render<TestableStructuredLogs>();
        cut.WaitForAssertion(() => Assert.Contains("initial authorized row", cut.Markup));

        cut.Instance.SetVisibleRowCap(1);
        await cut.InvokeAsync(() => observer.EmitLogAsync(Log("live authorized row")));
        cut.WaitForAssertion(() => Assert.Contains("live authorized row", cut.Markup));
        await cut.InvokeAsync(cut.Instance.SelectCurrentRowAsync);
        cut.WaitForAssertion(() => Assert.Contains("structured-log-details", cut.Markup));

        Assert.Equal(1, cut.Instance.LocalDroppedRows);
        Assert.Equal(5, cut.Instance.BackendDroppedCount);
        Assert.Equal(7, cut.Instance.StorageDroppedWriteCount);
        Assert.Equal("live", cut.Instance.SelectedEventId);
        Assert.Equal(1, cut.Instance.SourceCount);

        var lateLogCallback = observer.CaptureLogCallback();
        var lateSourceCallback = observer.CaptureSourceCallback();
        var lateDroppedCallback = observer.CaptureDroppedCallback();

        if (source == "rest")
            await cut.InvokeAsync(() => cut.Instance.ChangeSourceAsync("other-source"));
        else
            await cut.InvokeAsync(() => observer.EmitStatusAsync(StructuredLogConnectionStatus.Unauthorized));

        cut.WaitForAssertion(() => Assert.Contains("You do not have permission to view structured logs.", cut.Markup));

        Assert.Equal(StructuredLogConnectionStatus.Unauthorized, cut.Instance.ConnectionStatus);
        Assert.Empty(cut.Instance.CurrentRows);
        Assert.Equal(0, cut.Instance.SourceCount);
        Assert.Null(cut.Instance.SelectedEventId);
        Assert.Equal(0, cut.Instance.LocalDroppedRows);
        Assert.Equal(0, cut.Instance.BackendDroppedCount);
        Assert.Equal(0, cut.Instance.StorageDroppedWriteCount);
        Assert.False(cut.Instance.HasStorageDiagnosticsProvider);
        Assert.Equal(1, observer.DisposeCalls);
        Assert.DoesNotContain("initial authorized row", cut.Markup);
        Assert.DoesNotContain("live authorized row", cut.Markup);
        Assert.DoesNotContain("secret-node-alpha", cut.Markup);
        Assert.DoesNotContain("structured-log-details", cut.Markup);

        await cut.InvokeAsync(async () =>
        {
            await lateLogCallback!(Log("late row"));
            await lateSourceCallback!(Source("late-source", "late source metadata"));
            await lateDroppedCallback!(new StructuredLogDroppedEventSummary { DroppedCount = 99 });
        });

        Assert.Empty(cut.Instance.CurrentRows);
        Assert.Equal(0, cut.Instance.SourceCount);
        Assert.Equal(0, cut.Instance.BackendDroppedCount);
        Assert.DoesNotContain("late row", cut.Markup);
        Assert.DoesNotContain("late source metadata", cut.Markup);

        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());
        Assert.Equal(1, observer.DisposeCalls);
    }

    private static StructuredLogEvent Log(string message) => new()
    {
        Id = message.StartsWith("live", StringComparison.Ordinal) ? "live" : message,
        Timestamp = DateTimeOffset.UtcNow,
        Level = StructuredLogLevel.Information,
        Category = "Tests",
        Message = message,
        SourceId = "node-alpha"
    };

    private static StructuredLogSource Source(string id, string displayName) => new()
    {
        Id = id,
        DisplayName = displayName,
        Status = StructuredLogSourceStatus.Connected,
        LastSeen = DateTimeOffset.UtcNow
    };

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? "", key ?? "");

        public LocalizedString this[string? key, params object[] arguments] =>
            new(key ?? "", string.Format(key ?? "", arguments));
    }

    private sealed class TestStructuredLogService(bool denyRefresh, HttpStatusCode denialStatus) : IStructuredLogService
    {
        private int _recentCalls;

        public async Task<RecentStructuredLogsResult> GetRecentAsync(StructuredLogFilter filter, int rowCap, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _recentCalls) > 1 && denyRefresh)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://backend.example/diagnostics/structured-logs");
                using var response = new HttpResponseMessage(denialStatus);
                throw await ApiException.Create(request, HttpMethod.Get, response, new RefitSettings());
            }

            return new RecentStructuredLogsResult
            {
                Items = [Log("initial authorized row")],
                DroppedEvents = 5
            };
        }

        public Task<ICollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ICollection<StructuredLogSource>>([Source("node-alpha", "secret-node-alpha")]);

        public Task<StructuredLogStorageDiagnostics> GetStorageDiagnosticsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new StructuredLogStorageDiagnostics
            {
                DroppedWriteCount = 7,
                HasStorageDiagnosticsProvider = true
            });
    }

    private sealed class TestStructuredLogObserver : IStructuredLogObserver
    {
        public event Func<StructuredLogEvent, Task>? LogReceived;
        public event Func<StructuredLogDroppedEventSummary, Task>? DroppedEventsReceived;
        public event Func<StructuredLogSource, Task>? SourceChanged;
        public event Func<StructuredLogConnectionStatus, Task>? ConnectionStatusChanged;

        public int DisposeCalls { get; private set; }

        public Task StartAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateFilterAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReconnectAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public async Task EmitLogAsync(StructuredLogEvent logEvent)
        {
            if (LogReceived is { } handler)
                await handler(logEvent);
        }

        public async Task EmitStatusAsync(StructuredLogConnectionStatus status)
        {
            if (ConnectionStatusChanged is { } handler)
                await handler(status);
        }

        public Func<StructuredLogEvent, Task>? CaptureLogCallback() => LogReceived;
        public Func<StructuredLogSource, Task>? CaptureSourceCallback() => SourceChanged;
        public Func<StructuredLogDroppedEventSummary, Task>? CaptureDroppedCallback() => DroppedEventsReceived;

        public ValueTask DisposeAsync()
        {
            DisposeCalls++;
            return ValueTask.CompletedTask;
        }
    }

    public sealed class TestableStructuredLogs : Elsa.Studio.Diagnostics.StructuredLogs.UI.Pages.StructuredLogs
    {
        public IReadOnlyList<StructuredLogEvent> CurrentRows => Rows;
        public int SourceCount => Sources.Count;
        public int LocalDroppedRows => ViewState.LocalDroppedRows;
        public long BackendDroppedCount => base.BackendDroppedCount;
        public long StorageDroppedWriteCount => base.StorageDroppedWriteCount;
        public bool HasStorageDiagnosticsProvider => base.HasStorageDiagnosticsProvider;
        public string? SelectedEventId => ViewState.SelectedEventId;
        public StructuredLogConnectionStatus ConnectionStatus => ViewState.ConnectionStatus;

        public void SetVisibleRowCap(int cap) => ViewState.VisibleRowCap = cap;
        public Task ChangeSourceAsync(string sourceId) => SetSourceAsync(sourceId);
        public Task SelectCurrentRowAsync() => SelectDetailsAsync(Rows.Last());
    }
}

public sealed class SignalRStructuredLogObserverAuthorizationTests
{
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task StartAsync_WhenTheHubHandshakeIsDenied_ReportsUnauthorized(HttpStatusCode statusCode)
    {
        var observer = new SignalRStructuredLogObserver(
            new TestBackendApiClientProvider(new Uri("https://backend.example/")),
            new DenyingHttpConnectionOptionsConfigurator(statusCode),
            NullLogger<SignalRStructuredLogObserver>.Instance);
        var statuses = new List<StructuredLogConnectionStatus>();
        observer.ConnectionStatusChanged += status =>
        {
            statuses.Add(status);
            return Task.CompletedTask;
        };

        await observer.StartAsync(new StructuredLogFilter());

        Assert.Contains(StructuredLogConnectionStatus.Unauthorized, statuses);
        await observer.DisposeAsync();
    }

    private sealed class TestBackendApiClientProvider(Uri url) : IBackendApiClientProvider
    {
        public Uri Url { get; } = url;

        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class =>
            ValueTask.FromException<T>(new NotSupportedException());
    }

    private sealed class DenyingHttpConnectionOptionsConfigurator(HttpStatusCode statusCode) : IHttpConnectionOptionsConfigurator
    {
        public Task ConfigureAsync(HttpConnectionOptions options, CancellationToken cancellationToken = default)
        {
            options.HttpMessageHandlerFactory = _ => new DenyingHandler(statusCode);
            return Task.CompletedTask;
        }
    }

    private sealed class DenyingHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }
}
