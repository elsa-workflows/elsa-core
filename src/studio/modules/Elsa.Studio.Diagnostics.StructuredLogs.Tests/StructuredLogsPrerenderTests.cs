using Bunit;
using Elsa.Studio.Contracts;
using Elsa.Studio.Diagnostics.StructuredLogs.Contracts;
using Elsa.Studio.Diagnostics.StructuredLogs.Models;
using Elsa.Studio.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using StructuredLogsPage = Elsa.Studio.Diagnostics.StructuredLogs.UI.Pages.StructuredLogs;
using Xunit;

namespace Elsa.Studio.Diagnostics.StructuredLogs.Tests;

public sealed class StructuredLogsPrerenderTests
{
    [Fact]
    public async Task Static_render_does_not_invoke_javascript()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMudServices();
        services.AddSingleton<ILocalizer, TestLocalizer>();
        services.AddSingleton<IStructuredLogService, TestStructuredLogService>();
        services.AddSingleton<IStructuredLogObserver, TestStructuredLogObserver>();
        var jsRuntime = new TrackingJsRuntime();
        services.AddSingleton<IJSRuntime>(jsRuntime);
        services.AddSingleton<NavigationManager, TestNavigationManager>();

        await using var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        await using var renderer = new HtmlRenderer(serviceProvider, loggerFactory);

        var rendered = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await renderer.RenderComponentAsync<MudPopoverProvider>(ParameterView.Empty);
            var page = await renderer.RenderComponentAsync<StructuredLogsPage>(ParameterView.Empty);
            return page.ToHtmlString();
        });

        Assert.DoesNotContain("import", jsRuntime.Invocations);
        Assert.DoesNotContain("scrollToBottomById", jsRuntime.Invocations);
        Assert.DoesNotContain("JavaScript interop is unavailable", rendered);
    }

    internal sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? "", key ?? "");

        public LocalizedString this[string? key, params object[] arguments] =>
            new(key ?? "", string.Format(key ?? "", arguments));
    }

    internal sealed class TestStructuredLogService : IStructuredLogService
    {
        public Task<RecentStructuredLogsResult> GetRecentAsync(StructuredLogFilter filter, int rowCap, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RecentStructuredLogsResult());

        public Task<ICollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ICollection<StructuredLogSource>>([]);

        public Task<StructuredLogStorageDiagnostics> GetStorageDiagnosticsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new StructuredLogStorageDiagnostics());
    }

    internal sealed class TestStructuredLogObserver : IStructuredLogObserver
    {
        public event Func<StructuredLogEvent, Task>? LogReceived { add { } remove { } }
        public event Func<StructuredLogDroppedEventSummary, Task>? DroppedEventsReceived { add { } remove { } }
        public event Func<StructuredLogSource, Task>? SourceChanged { add { } remove { } }
        public event Func<StructuredLogConnectionStatus, Task>? ConnectionStatusChanged { add { } remove { } }

        public Task StartAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateFilterAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReconnectAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class TrackingJsRuntime : IJSRuntime
    {
        public List<string> Invocations { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Invocations.Add(identifier);
            return identifier == "import"
                ? ValueTask.FromException<TValue>(new InvalidOperationException("JavaScript interop is unavailable during static rendering."))
                : ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, args);
        }
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/diagnostics/structured-logs");

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }
}

public sealed class StructuredLogsInteractiveScrollTests : BunitContext, IAsyncLifetime
{
    private const string JsModule = "./_content/Elsa.Studio.Diagnostics.StructuredLogs/structuredLogs.js";

    public StructuredLogsInteractiveScrollTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, StructuredLogsPrerenderTests.TestLocalizer>();
        Services.AddSingleton<IStructuredLogService, StructuredLogsPrerenderTests.TestStructuredLogService>();
        Services.AddSingleton<IStructuredLogObserver, StructuredLogsPrerenderTests.TestStructuredLogObserver>();
    }

    [Fact]
    public void Initial_load_scrolls_after_interactive_render()
    {
        var module = JSInterop.SetupModule(JsModule);
        module.SetupVoid("scrollToBottomById", _ => true).SetVoidResult();
        Render<MudPopoverProvider>();

        Render<StructuredLogsPage>();

        Assert.Contains(module.Invocations, invocation => invocation.Identifier == "scrollToBottomById");
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();
}
