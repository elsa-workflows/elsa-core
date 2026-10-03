using System.Net;
using System.Security.Claims;
using Bunit;
using Elsa.Studio.Authorization;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Dashboard.Client;
using Elsa.Studio.Dashboard.Components;
using Elsa.Studio.Dashboard.Menu;
using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Services;
using Elsa.Studio.Dashboard.Widgets;
using Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard.UI.Dashboard;
using Elsa.Studio.Diagnostics.StructuredLogs.Dashboard.UI.Dashboard;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Dashboard.Widgets;
using Elsa.Studio.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Studio.Diagnostics.OpenTelemetry.Dashboard.UI.Dashboard;
using Elsa.Studio.Diagnostics.OpenTelemetry.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Refit;
using Xunit;
using DashboardPage = Elsa.Studio.Dashboard.Pages.Index;

namespace Elsa.Studio.Dashboard.Tests;

/// <summary>
/// Every signed-in user can open the dashboard. It shows a user only the widgets they may see, requests only the data those
/// widgets need from endpoints the user may call, and welcomes a user who may see none with shortcuts instead.
/// </summary>
public sealed class DashboardPagePermissionTests : BunitContext, IAsyncLifetime
{
    private const string OverviewEndpoint = "overview";
    private const string WelcomeWithShortcuts = "No dashboard widgets are available to your role. These are the pages you can open:";
    private static readonly string[] EveryEndpoint = ["needs-attention", OverviewEndpoint, "recent-activity", "workflow-hotspots", "workflow-trends"];
    private static readonly Type[] WorkflowInstanceWidgets = [typeof(DashboardWorkflowMetricsWidget), typeof(DashboardNeedsAttentionWidget), typeof(DashboardTrendWidget), typeof(DashboardRecentActivityWidget), typeof(DashboardWorkflowHotspotsWidget)];
    private static readonly Type[] DiagnosticsWidgets = [typeof(StructuredLogsDashboardWidget), typeof(ConsoleLogsDashboardWidget)];

    private readonly RecordingDashboardApi _api = new();
    private readonly StubFeatureService _features = new();
    private readonly DashboardWidgetRegistry _registry = new();
    private readonly StubPermissionService _permissions = new(UserPermissions.Unknown);
    private readonly ManualTimeProvider _time = new();
    private readonly TestAuthenticationStateProvider _authentication = new();

    public DashboardPagePermissionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IDashboardService>(new DashboardService(new StubBackend(_api)));
        Services.AddSingleton<IDashboardWidgetRegistry>(_registry);
        Services.AddSingleton<IEnumerable<DashboardWidgetDescriptor>>([]);
        Services.AddSingleton<IFeatureService>(_features);
        Services.AddSingleton<IPermissionService>(_permissions);
        Services.AddSingleton<TimeProvider>(_time);
        Services.AddSingleton<AuthenticationStateProvider>(_authentication);
        Services.AddSingleton<IOpenTelemetryService>(new StubOpenTelemetryService());
        Services.AddSingleton<IMenuService>(new DefaultMenuService([new DashboardMenu(new TestLocalizer()), new NavigationMenu()], [new DefaultMenuGroupProvider()], _permissions));
        Render<MudPopoverProvider>();
    }

    [Fact]
    public async Task AUserWhoCanViewWorkflowInstances_SeesTheInstanceWidgetsOnly_AndLoadsNothingElse()
    {
        var cut = await RenderDashboardAsync("workflows/instances:view");

        cut.WaitForAssertion(() => Assert.Equal(Names(WorkflowInstanceWidgets), ShownWidgets(cut)));
        Assert.Empty(cut.FindComponents<DashboardRuntimeChip>());
        Assert.Equal(EveryEndpoint, _api.Calls.Order());
    }

    [Theory]
    [InlineData("diagnostics/structured-logs:view", typeof(StructuredLogsDashboardWidget))]
    [InlineData("diagnostics/console-logs:view", typeof(ConsoleLogsDashboardWidget))]
    public async Task AUserWhoCanViewOneLogSummary_SeesItsWidget_AndRequestsOnlyTheOverview(string grant, Type widget)
    {
        var cut = await RenderDashboardAsync(grant);

        cut.WaitForAssertion(() => Assert.Equal(Names(widget), ShownWidgets(cut)));
        Assert.Equal([OverviewEndpoint], _api.Calls);
    }

    [Theory]
    [InlineData("dashboard:view")]
    [InlineData(null)]
    public async Task ADashboardViewer_AndAUserWhosePermissionsAreUnknown_SeeEverything(string? grant)
    {
        var cut = await RenderDashboardAsync(grant == null ? UserPermissions.Unknown : StubPermissionService.Grants(grant));

        cut.WaitForAssertion(() => Assert.Equal(Names([.. WorkflowInstanceWidgets, .. DiagnosticsWidgets]), ShownWidgets(cut)));
        Assert.Single(cut.FindComponents<DashboardRuntimeChip>());
        Assert.Equal(EveryEndpoint, _api.Calls.Order());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TheRuntimeStatus_ShowsOnlyToUsersWhoMayReadIt(bool canViewRuntime)
    {
        string[] grants = canViewRuntime ? ["workflows/instances:view", "workflows/runtime:view"] : ["workflows/instances:view"];

        var cut = await RenderDashboardAsync(grants);

        cut.WaitForAssertion(() => Assert.NotEmpty(ShownWidgets(cut)));
        Assert.Equal(canViewRuntime, cut.FindComponents<DashboardRuntimeChip>().Count == 1);
    }

    [Fact]
    public async Task ASectionTheBackendWithholds_IsLeftOut_RatherThanReported()
    {
        _api.Overview = new()
        {
            Runtime = new() { Capability = DashboardCapabilityStatus.Unauthorized },
            WorkflowInstances = new() { Capability = DashboardCapabilityStatus.Unauthorized },
            Diagnostics = new()
            {
                StructuredLogs = new() { Capability = DashboardCapabilityStatus.Unauthorized },
                ConsoleLogs = new() { Capability = DashboardCapabilityStatus.Unauthorized }
            }
        };
        _api.NeedsAttention = new() { Capability = DashboardCapabilityStatus.Unauthorized };

        var cut = await RenderDashboardAsync(UserPermissions.Unknown);

        cut.WaitForAssertion(() => Assert.Equal(Names(typeof(DashboardTrendWidget), typeof(DashboardRecentActivityWidget), typeof(DashboardWorkflowHotspotsWidget)), ShownWidgets(cut)));
        Assert.Empty(cut.FindComponents<DashboardRuntimeChip>());
        Assert.DoesNotContain(DashboardUiMapper.CapabilityLabel(DashboardCapabilityStatus.Unauthorized), cut.Markup);
    }

    [Fact]
    public async Task WithoutAWidgetToShow_TheWelcomePanelLinksToThePagesTheUserCanOpen_InNavigationOrder()
    {
        var cut = await RenderDashboardAsync("secrets:view", "workflows/definitions:view");

        // Secrets is listed first but belongs to a later group; the dashboard itself is not a shortcut.
        cut.WaitForAssertion(() => Assert.Equal(["workflows/definitions", "security/secrets"], cut.FindAll("[data-testid='dashboard-welcome'] a").Select(x => x.GetAttribute("href"))));
        Assert.Contains(WelcomeWithShortcuts, cut.Find("[data-testid='dashboard-welcome']").TextContent);
        Assert.Empty(_api.Calls);
        Assert.Empty(cut.FindAll("[data-testid='access-denied']"));
        Assert.Equal("http://localhost/", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public async Task WithoutAWidgetOrAPageToOpen_TheNoPagesNoticeIsShown()
    {
        var cut = await RenderDashboardAsync("unrelated:view");

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='no-accessible-pages']")));
        Assert.Empty(cut.FindAll("[data-testid='dashboard-welcome']"));
        Assert.Empty(_api.Calls);
    }

    [Theory]
    [InlineData("dashboard:view", false)]
    [InlineData("secrets:view", true)]
    public async Task WhileTheWidgetsAreStillBeingRegistered_TheDashboardIsLoading_NotWelcoming(string grant, bool welcomed)
    {
        var cut = RenderDashboard(StubPermissionService.Grants(grant));

        Assert.Contains("Loading dashboard", cut.Find(".mud-chip").TextContent);
        Assert.Empty(cut.FindAll("[data-testid='dashboard-welcome']"));
        Assert.Empty(_api.Calls);

        await InitializeFeaturesAsync();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(welcomed, cut.FindAll("[data-testid='dashboard-welcome']").Count == 1);
            Assert.Equal(!welcomed, ShownWidgets(cut).Count > 0);
            Assert.Equal(!welcomed, _api.Calls.Count > 0);
        });
    }

    [Fact]
    public async Task TheDashboard_IsNotGated()
    {
        Assert.Empty(RequirePermissionAttribute.GetRequiredPermissions(typeof(DashboardPage)));
        Assert.All(await new DashboardMenu(new TestLocalizer()).GetMenuItemsAsync(), item => Assert.Empty(item.RequiredPermissions));
    }

    [Theory]
    [InlineData("acme/things:view", true)]
    [InlineData("secrets:view", false)]
    public async Task AContributedWidget_ShowsOnlyToUsersHoldingThePermissionItDeclares(string grant, bool shown)
    {
        _registry.Add(new("acme.things", DashboardWidgetZones.SecondaryPanels, 10, typeof(AcmeWidget)) { RequiredPermissions = [new("acme/things", PermissionVerbs.View)] });

        var cut = await RenderDashboardAsync(grant);

        IReadOnlyList<string> expectedWidgets = shown ? Names(typeof(AcmeWidget)) : [];
        string[] expectedCalls = shown ? [OverviewEndpoint] : [];
        cut.WaitForAssertion(() => Assert.Equal(expectedWidgets, ShownWidgets(cut)));
        Assert.Equal(expectedCalls, _api.Calls);
    }

    [Fact]
    public async Task AContributedWidgetDeclaringNoPermission_ShowsToEveryone()
    {
        _registry.Add(new("acme.status", DashboardWidgetZones.SecondaryPanels, 10, typeof(AcmeWidget)));

        var cut = await RenderDashboardAsync("secrets:view");

        cut.WaitForAssertion(() => Assert.Equal(Names(typeof(AcmeWidget)), ShownWidgets(cut)));
    }

    [Fact]
    public async Task ADashboardViewer_OnAHostWithOnlyDiagnosticsWidgets_RequestsOnlyTheOverview()
    {
        var cut = await RenderDashboardAsync(StubPermissionService.Grants("dashboard:view"), includeWorkflows: false);

        cut.WaitForAssertion(() => Assert.Equal(Names(DiagnosticsWidgets), ShownWidgets(cut)));
        Assert.Equal([OverviewEndpoint], _api.Calls);
    }

    [Fact]
    public async Task ADashboardViewer_DoesNotSeeTheOpenTelemetryWidget_WhoseDataComesFromTheOpenTelemetryApi()
    {
        await new Elsa.Studio.Diagnostics.OpenTelemetry.Dashboard.Feature(_registry).InitializeAsync();

        var cut = await RenderDashboardAsync("dashboard:view");

        cut.WaitForAssertion(() => Assert.Equal(Names([.. WorkflowInstanceWidgets, .. DiagnosticsWidgets]), ShownWidgets(cut)));
    }

    [Fact]
    public async Task AnOpenTelemetryOnlyUser_SeesOnlyTheOpenTelemetryWidget_AndCallsNoInstanceEndpoint()
    {
        await new Elsa.Studio.Diagnostics.OpenTelemetry.Dashboard.Feature(_registry).InitializeAsync();

        var cut = await RenderDashboardAsync("diagnostics/opentelemetry:view");

        cut.WaitForAssertion(() => Assert.Equal(Names(typeof(OpenTelemetryDashboardWidget)), ShownWidgets(cut)));
        Assert.Empty(cut.FindAll("[data-testid='dashboard-welcome']"));
        Assert.Equal([OverviewEndpoint], _api.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ARuntimeOnlyUser_SeesTheRuntimeStatusAboveTheWelcomeShortcuts(bool runtimeWithheld)
    {
        if (runtimeWithheld)
            _api.Overview = new() { Runtime = new() { Capability = DashboardCapabilityStatus.Unauthorized } };

        var cut = await RenderDashboardAsync("workflows/runtime:view", "secrets:view");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(["security/secrets"], cut.FindAll("[data-testid='dashboard-welcome'] a").Select(x => x.GetAttribute("href")));
            Assert.Contains(WelcomeWithShortcuts, cut.Find("[data-testid='dashboard-welcome']").TextContent);
            Assert.Equal(!runtimeWithheld, cut.FindComponents<DashboardRuntimeChip>().Count == 1);
            Assert.Equal(!runtimeWithheld, cut.FindAll(".dashboard-header").Count == 1);
        });
        Assert.Empty(ShownWidgets(cut));
        Assert.Equal([OverviewEndpoint], _api.Calls);
    }

    [Fact]
    public async Task WhenTheBackendRefusesAnInstanceEndpointToAUserWithUnknownPermissions_TheOverviewStays()
    {
        _api.Refused.UnionWith(["needs-attention", "workflow-trends", "recent-activity", "workflow-hotspots"]);

        var cut = await RenderDashboardAsync(UserPermissions.Unknown);

        // Only what the overview carries is shown; the refused parts are absent, and the page does not report "No access".
        cut.WaitForAssertion(() => Assert.Equal(Names(typeof(DashboardWorkflowMetricsWidget), typeof(StructuredLogsDashboardWidget), typeof(ConsoleLogsDashboardWidget)), ShownWidgets(cut)));
        Assert.Single(cut.FindComponents<DashboardRuntimeChip>());
        Assert.DoesNotContain("No access", cut.Markup);
        Assert.DoesNotContain("Needs attention", cut.Markup);
    }

    [Fact]
    public async Task WhenTheBackendRefusesTheOverview_TheDashboardReportsNoAccess()
    {
        _api.Refused.Add(OverviewEndpoint);

        var cut = await RenderDashboardAsync(UserPermissions.Unknown);

        cut.WaitForAssertion(() => Assert.Contains("No access", cut.Markup));
        Assert.Empty(ShownWidgets(cut));
    }

    [Fact]
    public async Task AChangeInPermissions_ThatWidensTheScope_LoadsTheInstanceData()
    {
        var cut = await RenderDashboardAsync("diagnostics/console-logs:view");
        cut.WaitForAssertion(() => Assert.Equal(Names(typeof(ConsoleLogsDashboardWidget)), ShownWidgets(cut)));
        Assert.Equal([OverviewEndpoint], _api.Calls);

        ChangePermissions("diagnostics/console-logs:view", "workflows/instances:view");

        cut.WaitForAssertion(() => Assert.Equal(Names([.. WorkflowInstanceWidgets, typeof(ConsoleLogsDashboardWidget)]), ShownWidgets(cut)));
        Assert.Equal(["needs-attention", "recent-activity", "workflow-hotspots", "workflow-trends"], _api.Calls.Except([OverviewEndpoint]).Order());
    }

    // Both grants need only the overview, but the backend withholds the other log section from each, so the data loaded
    // for the first grant cannot serve the second.
    [Fact]
    public async Task AChangeInPermissions_ThatKeepsTheScope_ReloadsTheOverview()
    {
        var cut = await RenderDashboardAsync("diagnostics/structured-logs:view");
        cut.WaitForAssertion(() => Assert.Equal(Names(typeof(StructuredLogsDashboardWidget)), ShownWidgets(cut)));
        Assert.Equal([OverviewEndpoint], _api.Calls);

        ChangePermissions("diagnostics/console-logs:view");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(Names(typeof(ConsoleLogsDashboardWidget)), ShownWidgets(cut));
            Assert.Equal([OverviewEndpoint, OverviewEndpoint], _api.Calls);
        });
    }

    // A renewed token resolves new permissions with the same grants; that is no reason to reload.
    [Fact]
    public async Task ARenewalWithTheSameGrants_DoesNotReload()
    {
        var cut = await RenderDashboardAsync("workflows/instances:view");
        cut.WaitForAssertion(() => Assert.Equal(Names(WorkflowInstanceWidgets), ShownWidgets(cut)));
        var callsBefore = _api.Calls.Count;

        ChangePermissions("workflows/instances:view");
        cut.WaitForAssertion(() => Assert.Equal(Names(WorkflowInstanceWidgets), ShownWidgets(cut)));

        Assert.Equal(callsBefore, _api.Calls.Count);
    }

    [Fact]
    public async Task AChangeInPermissions_ThatRemovesEveryWidget_ShowsTheWelcome()
    {
        var cut = await RenderDashboardAsync("workflows/instances:view", "secrets:view");
        cut.WaitForAssertion(() => Assert.Equal(Names(WorkflowInstanceWidgets), ShownWidgets(cut)));
        var callsBefore = _api.Calls.Count;

        ChangePermissions("secrets:view");

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(ShownWidgets(cut));
            Assert.Equal(["security/secrets"], cut.FindAll("[data-testid='dashboard-welcome'] a").Select(x => x.GetAttribute("href")));
        });
        Assert.Equal(callsBefore, _api.Calls.Count);
    }

    [Fact]
    public void WhenTheFeaturesNeverInitialize_TheDashboardStopsWaiting_AndStillTakesLateWidgets()
    {
        var cut = RenderDashboard(StubPermissionService.Grants("secrets:view"));
        Assert.Contains("Loading dashboard", cut.Find(".mud-chip").TextContent);

        _time.FireTimers();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='dashboard-welcome']")));
        Assert.Equal(TimeSpan.FromSeconds(3), _time.LastDueTime);
        Assert.Empty(_api.Calls);

        // A decorating feature service that never reports itself initialized, but does raise the event once it is.
        _registry.Add(new("acme.status", DashboardWidgetZones.SecondaryPanels, 10, typeof(AcmeWidget)));
        _features.CompleteInitialization();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(Names(typeof(AcmeWidget)), ShownWidgets(cut));
            Assert.Empty(cut.FindAll("[data-testid='dashboard-welcome']"));
        });
    }

    [Fact]
    public async Task WhenTheDashboardIsDisposed_ItDisposesItsTimer_AndStopsListeningForTheFeatures()
    {
        var page = RenderDashboard(StubPermissionService.Grants("workflows/instances:view")).FindComponent<DashboardPage>().Instance;
        Assert.Equal(1, _features.SubscriberCount);
        Assert.Equal(1, _time.LiveTimers);

        await page.DisposeAsync();

        Assert.Equal(0, _features.SubscriberCount);
        Assert.Equal(0, _time.LiveTimers);
    }

    // The callbacks may already be in flight when the page goes away: the timer callback, or the feature service's event
    // raised from a copy of its subscribers. Neither may load on, render to, or throw from a disposed page.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AfterTheDashboardIsDisposed_TheTimerAndTheInitializedEvent_LoadNothingAndThrowNothing(bool viaTimer)
    {
        var cut = RenderDashboard(StubPermissionService.Grants("workflows/instances:view"));
        var page = cut.FindComponent<DashboardPage>().Instance;
        await new Elsa.Studio.Workflows.Dashboard.Feature(_registry).InitializeAsync();
        await page.DisposeAsync();

        if (viaTimer)
            _time.FireEveryTimer();
        else
            _features.RaiseToEveryoneEverSubscribed();

        await cut.InvokeAsync(() => { });
        Assert.Empty(_api.Calls);
    }

    // The load a callback started may still be running when the page is disposed: it ends without rendering to the page.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WhenTheDashboardIsDisposedWhileALoadStartedByACallbackIsInFlight_TheLoadEndsQuietly(bool viaTimer)
    {
        var cut = RenderDashboard(StubPermissionService.Grants("workflows/instances:view"));
        var page = cut.FindComponent<DashboardPage>().Instance;
        await new Elsa.Studio.Workflows.Dashboard.Feature(_registry).InitializeAsync();
        var gate = _api.HoldOverview();

        if (viaTimer)
            _time.FireTimers();
        else
            _features.RaiseToEveryoneEverSubscribed();

        Assert.NotEmpty(_api.Calls);
        var unobserved = new List<Exception>();
        void Observe(object? sender, UnobservedTaskExceptionEventArgs e) => unobserved.Add(e.Exception);
        TaskScheduler.UnobservedTaskException += Observe;

        try
        {
            await page.DisposeAsync();
            gate.SetResult();
            await cut.InvokeAsync(() => { });
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= Observe;
        }

        Assert.Empty(unobserved);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    private Task<IRenderedComponent<PermissionPageGuard>> RenderDashboardAsync(params string[] grants) =>
        RenderDashboardAsync(StubPermissionService.Grants(grants));

    private async Task<IRenderedComponent<PermissionPageGuard>> RenderDashboardAsync(UserPermissions permissions, bool includeWorkflows = true)
    {
        await InitializeFeaturesAsync(includeWorkflows);
        return RenderDashboard(permissions);
    }

    // As when the user's token is refreshed with different grants.
    private void ChangePermissions(params string[] grants)
    {
        _permissions.Permissions = StubPermissionService.Grants(grants);
        _authentication.NotifyChanged();
    }

    // Rendered the way the shell renders a page: inside the guard that enforces its declared permissions.
    private IRenderedComponent<PermissionPageGuard> RenderDashboard(UserPermissions permissions)
    {
        _permissions.Permissions = permissions;

        return Render<PermissionPageGuard>(parameters => parameters
            .AddCascadingValue(new RouteData(typeof(DashboardPage), new Dictionary<string, object?>()))
            .AddChildContent<DashboardPage>());
    }

    // As the shell does once the user has signed in: the dashboard companions register their widgets, then the feature
    // service reports it is done.
    private async Task InitializeFeaturesAsync(bool includeWorkflows = true)
    {
        IEnumerable<IFeature> companions =
        [
            new Elsa.Studio.Diagnostics.StructuredLogs.Dashboard.Feature(_registry),
            new Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard.Feature(_registry)
        ];

        if (includeWorkflows)
            companions = companions.Prepend(new Elsa.Studio.Workflows.Dashboard.Feature(_registry));

        foreach (var companion in companions)
            await companion.InitializeAsync();

        _features.CompleteInitialization();
    }

    // The widgets that rendered anything: a widget whose data the backend withholds renders nothing.
    private static IReadOnlyList<string> ShownWidgets(IRenderedComponent<PermissionPageGuard> cut) => cut
        .FindComponents<DynamicComponent>()
        .Where(x => !string.IsNullOrWhiteSpace(x.Markup))
        .Select(x => x.Instance.Type.Name)
        .Order()
        .ToList();

    private static IReadOnlyList<string> Names(params Type[] widgets) => widgets.Select(x => x.Name).Order().ToList();

    private sealed class AcmeWidget : ComponentBase
    {
        [Parameter] public DashboardWidgetContext Context { get; set; } = null!;
        [Parameter] public DashboardWidgetDescriptor Descriptor { get; set; } = null!;

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) => builder.AddContent(0, "Acme");
    }

    private sealed class RecordingDashboardApi : IDashboardApi
    {
        public List<string> Calls { get; } = [];
        public DashboardOverview Overview { get; set; } = new();
        public DashboardNeedsAttentionResponse NeedsAttention { get; set; } = new();
        public HashSet<string> Refused { get; } = [];
        private TaskCompletionSource? _overviewGate;

        // Holds the overview until the returned source is completed, and ignores cancellation meanwhile.
        public TaskCompletionSource HoldOverview() => _overviewGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<DashboardOverview> GetOverviewAsync(string? range = null, bool includeSystem = false, CancellationToken cancellationToken = default) => Record(OverviewEndpoint, Overview);
        public Task<DashboardTrendResponse> GetWorkflowTrendsAsync(DashboardTrendRequest request, CancellationToken cancellationToken = default) => Record("workflow-trends", new DashboardTrendResponse());
        public Task<DashboardNeedsAttentionResponse> GetNeedsAttentionAsync(string? range = null, int take = 8, bool includeSystem = false, CancellationToken cancellationToken = default) => Record("needs-attention", NeedsAttention);
        public Task<DashboardRecentActivityResponse> GetRecentActivityAsync(string? range = null, int take = 20, bool includeSystem = false, CancellationToken cancellationToken = default) => Record("recent-activity", new DashboardRecentActivityResponse());
        public Task<DashboardWorkflowHotspotsResponse> GetWorkflowHotspotsAsync(DashboardWorkflowHotspotsRequest request, CancellationToken cancellationToken = default) => Record("workflow-hotspots", new DashboardWorkflowHotspotsResponse());

        private async Task<T> Record<T>(string endpoint, T response)
        {
            Calls.Add(endpoint);

            if (endpoint == OverviewEndpoint && _overviewGate != null)
                await _overviewGate.Task;

            if (!Refused.Contains(endpoint))
                return response;

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://elsa.example.test/dashboard");
            using var refusal = new HttpResponseMessage(HttpStatusCode.Forbidden);
            throw await ApiException.Create(request, HttpMethod.Get, refusal, new RefitSettings());
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private readonly List<ManualTimer> _timers = [];

        public TimeSpan LastDueTime { get; private set; }

        public int LiveTimers => _timers.Count(x => !x.Disposed);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            LastDueTime = dueTime;
            var timer = new ManualTimer(() => callback(state));
            _timers.Add(timer);
            return timer;
        }

        public void FireTimers()
        {
            foreach (var timer in _timers.Where(x => !x.Disposed).ToList())
                timer.Fire();
        }

        // As a timer whose callback was already running when it was disposed.
        public void FireEveryTimer()
        {
            foreach (var timer in _timers.ToList())
                timer.Fire();
        }

        private sealed class ManualTimer(Action callback) : ITimer
        {
            public bool Disposed { get; private set; }
            public void Fire() => callback();
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;
            public void Dispose() => Disposed = true;
            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }

    private sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

        public void NotifyChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private sealed class StubOpenTelemetryService : IOpenTelemetryService
    {
        public Task<OpenTelemetryStorageDiagnostics> GetStorageDiagnosticsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new OpenTelemetryStorageDiagnostics(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        public Task<OpenTelemetryResourceResult> GetResourcesAsync(OpenTelemetryResourceFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<OpenTelemetryTraceResult> GetTracesAsync(OpenTelemetryTraceFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<OpenTelemetryTraceDetail?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<OpenTelemetryMetricResult> GetMetricsAsync(OpenTelemetryMetricFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<OpenTelemetryLogResult> GetLogsAsync(OpenTelemetryLogFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CollectorConfiguration?> GetCollectorConfigurationAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubBackend(IDashboardApi api) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://elsa.example.test/");

        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class => ValueTask.FromResult((T)api);
    }

    // Navigation shaped like the built-in modules': Secrets is listed first but belongs to a later group.
    private sealed class NavigationMenu : IMenuProvider
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default) => new(new[]
        {
            Item("Secrets", "security/secrets", MenuItemGroups.Administration, "secrets"),
            new MenuItem
            {
                Text = "Workflows", Href = "", GroupName = MenuItemGroups.General.Name, Order = 10,
                SubMenuItems = [Item("Definitions", "workflows/definitions", MenuItemGroups.General, "workflows/definitions")]
            }
        });

        private static MenuItem Item(string text, string href, MenuItemGroup group, string resource) => new()
        {
            Text = text, Href = href, GroupName = group.Name, RequiredPermissions = [new(resource, PermissionVerbs.View)]
        };
    }
}
