using Bunit;
using Elsa.Studio.Contracts;
using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Services;
using Elsa.Studio.Dashboard.Widgets;
using Elsa.Studio.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using DashboardPage = Elsa.Studio.Dashboard.Pages.Index;

namespace Elsa.Studio.Dashboard.Tests;

/// <summary>
/// The dashboard header reports "unavailable" only once a load has actually failed, not while the first snapshot is on its way.
/// </summary>
public sealed class DashboardStatusTests : BunitContext, IAsyncLifetime
{
    private readonly List<TaskCompletionSource<DashboardLoadResult>> _loads = [];

    public DashboardStatusTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IDashboardService>(new StubDashboardService(_loads));
        Services.AddSingleton<IDashboardWidgetRegistry, DashboardWidgetRegistry>();
        Services.AddSingleton<IFeatureService, StubFeatureService>();
        Services.AddSingleton<IEnumerable<DashboardWidgetDescriptor>>([]);
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void WhileTheFirstSnapshotLoads_ShowsALoadingState()
    {
        var cut = Render<DashboardPage>();

        Assert.Contains("Loading dashboard", cut.Find(".mud-chip").TextContent);
        Assert.DoesNotContain("unavailable", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mud-chip-color-error", cut.Find(".mud-chip").ClassName);
    }

    [Theory]
    [InlineData(false, "Dashboard unavailable")]
    [InlineData(true, "Refresh failed")]
    public void WhenTheLoadFails_ShowsAnErrorStatus(bool throws, string expectedStatus)
    {
        var cut = Render<DashboardPage>();

        if (throws)
            _loads[0].SetException(new InvalidOperationException("The backend is unreachable."));
        else
            _loads[0].SetResult(DashboardLoadResult.Unavailable("Dashboard data is not available from this backend."));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(expectedStatus, cut.Find(".mud-chip").TextContent);
            Assert.Contains("mud-chip-color-error", cut.Find(".mud-chip").ClassName);
        });
    }

    [Fact]
    public void WhileARetryAfterAFailureLoads_ShowsTheLoadingStateAgain()
    {
        var cut = Render<DashboardPage>();
        _loads[0].SetResult(DashboardLoadResult.Unavailable("Dashboard data is not available from this backend."));
        cut.WaitForAssertion(() => Assert.Contains("Dashboard unavailable", cut.Find(".mud-chip").TextContent));

        cut.Find("button[aria-label='Refresh dashboard']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, _loads.Count);
            Assert.Contains("Loading dashboard", cut.Find(".mud-chip").TextContent);
            Assert.DoesNotContain("mud-chip-color-error", cut.Find(".mud-chip").ClassName);
        });
    }

    /// <summary>Hands out a separately controlled pending load for every request.</summary>
    private sealed class StubDashboardService(List<TaskCompletionSource<DashboardLoadResult>> loads) : IDashboardService
    {
        public Task<DashboardLoadResult> LoadAsync(string range, bool includeSystem = false, CancellationToken cancellationToken = default)
        {
            var load = new TaskCompletionSource<DashboardLoadResult>();
            loads.Add(load);
            return load.Task;
        }

        public Task<DashboardLoadResult<DashboardOverview>> LoadOverviewAsync(string range, bool includeSystem = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubFeatureService : IFeatureService
    {
        public event Action? Initialized { add { } remove { } }
        public IEnumerable<IFeature> GetFeatures() => [];
        public Task InitializeFeaturesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
