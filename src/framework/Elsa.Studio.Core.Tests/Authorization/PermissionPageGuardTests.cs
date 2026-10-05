using Bunit;
using Elsa.Studio.Authorization;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Elsa.Studio.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Core.Tests.Authorization;

public sealed class PermissionPageGuardTests : BunitContext, IAsyncLifetime
{
    private const string PageContent = "page-content";
    private const string InstancesPath = "workflows/instances";

    // While set, the landing menu does not resolve until it completes.
    private Task? _menuHold;

    public PermissionPageGuardTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        UseMenu();
    }

    [Fact]
    public void AnUngatedPage_RendersWhateverTheUserHolds()
    {
        var cut = RenderGuard<UngatedPage>(new StubPermissionService("secrets:view"));

        Assert.Contains(PageContent, cut.Markup);
    }

    [Fact]
    public void ThePage_ReceivesTheResolvedPermissions()
    {
        Services.AddSingleton<IPermissionService>(new StubPermissionService("secrets:view"));

        var cut = Render<PermissionPageGuard>(parameters => parameters
            .AddCascadingValue(Route<UngatedPage>())
            .AddChildContent<PermissionsConsumer>());

        var permissions = cut.FindComponent<PermissionsConsumer>().Instance.Permissions;
        Assert.NotNull(permissions);
        Assert.True(permissions.Has("secrets", PermissionVerbs.View));
        Assert.False(permissions.Has("secrets", PermissionVerbs.Delete));
    }

    [Fact]
    public void Navigating_ReusesTheResolvedPermissions()
    {
        var permissions = new StubPermissionService("workflows/instances:view");
        UsePermissions(permissions, "workflows/instances");
        var cut = Render<CascadingValue<RouteData>>(RouteTo<UngatedPage>);
        Assert.Contains(PageContent, cut.Markup);

        cut.Render(RouteTo<WorkflowInstancesPage>);

        Assert.Contains("workflows/definitions:view", cut.Find("[data-testid='access-denied']").TextContent);
        Assert.Equal(1, permissions.Calls);
    }

    [Fact]
    public void AGatedPage_RendersWhenTheUserHoldsEveryRequiredPermission()
    {
        var cut = RenderGuard<WorkflowInstancesPage>(new StubPermissionService("workflows/*:view"));

        Assert.Contains(PageContent, cut.Markup);
        Assert.Empty(cut.FindAll("[data-testid='access-denied']"));
    }

    [Fact]
    public void AGatedPage_RendersAccessDeniedInsteadOfThePage_WhenAPermissionIsMissing()
    {
        var cut = RenderGuard<WorkflowInstancesPage>(new StubPermissionService("secrets:view", "workflows/instances:view"));

        Assert.DoesNotContain(PageContent, cut.Markup);
        var alert = cut.Find("[data-testid='access-denied']");
        Assert.Contains("workflows/definitions:view", alert.TextContent);
        Assert.DoesNotContain("workflows/instances:view", alert.TextContent);
    }

    [Fact]
    public void AGatedPage_RendersAsBefore_WhenPermissionsAreUnknown()
    {
        var cut = RenderGuard<WorkflowInstancesPage>(new StubPermissionService(UserPermissions.Unknown));

        Assert.Contains(PageContent, cut.Markup);
    }

    [Fact]
    public void WithoutRouteData_TheContentRenders()
    {
        Services.AddSingleton<IPermissionService>(new StubPermissionService());

        var cut = Render<PermissionPageGuard>(parameters => parameters.AddChildContent(PageContent));

        Assert.Contains(PageContent, cut.Markup);
    }

    [Fact]
    public void AMountedPage_FollowsPermissionChangesWhenTheAuthenticationStateChanges()
    {
        var permissions = new StubPermissionService("workflows/*:view");
        var authentication = new NotifyingAuthenticationStateProvider();
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        var cut = RenderGuard<WorkflowInstancesPage>(permissions);
        Assert.Contains(PageContent, cut.Markup);

        permissions.Permissions = StubPermissionService.Grants("secrets:view");
        authentication.Notify();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='access-denied']")));

        permissions.Permissions = StubPermissionService.Grants("workflows/*:view");
        authentication.Notify();
        cut.WaitForAssertion(() => Assert.Contains(PageContent, cut.Markup));
    }

    [Fact]
    public void TheLandingPage_SendsAUserWhoCannotViewItToTheirFirstAccessiblePage()
    {
        var cut = RenderGuard<DashboardPage>(new StubPermissionService("secrets:view"), "/");

        cut.WaitForAssertion(() => Assert.Equal("http://localhost/security/secrets", Navigation.Uri));
        Assert.DoesNotContain(PageContent, cut.Markup);
        Assert.Empty(cut.FindAll("[data-testid='access-denied']"));
    }

    [Fact]
    public void TheLandingPage_PicksTheFirstAccessiblePageInNavigationOrder()
    {
        // Secrets sorts first by item order but sits in a later group, so the navigation lists Workflows above it.
        var cut = RenderGuard<DashboardPage>(new StubPermissionService("secrets:view", "workflows/definitions:view"), "/");

        cut.WaitForAssertion(() => Assert.Equal("http://localhost/workflows/definitions", Navigation.Uri));
    }

    [Fact]
    public void TheLandingPage_RendersForAUserWhoCanViewIt()
    {
        var cut = RenderGuard<DashboardPage>(new StubPermissionService("dashboard:view"), "/");

        Assert.Contains(PageContent, cut.Markup);
        Assert.Equal("http://localhost/", Navigation.Uri);
    }

    [Fact]
    public void TheLandingPage_RendersAsBefore_WhenPermissionsAreUnknown()
    {
        var cut = RenderGuard<DashboardPage>(new StubPermissionService(UserPermissions.Unknown), "/");

        Assert.Contains(PageContent, cut.Markup);
        Assert.Equal("http://localhost/", Navigation.Uri);
    }

    [Fact]
    public void TheLandingPage_ExplainsThatNoPagesAreAvailable_WhenTheUserCanOpenNone()
    {
        var cut = RenderGuard<DashboardPage>(new StubPermissionService("unrelated:view"), "/");

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='no-accessible-pages']")));
        Assert.Empty(cut.FindAll("[data-testid='access-denied']"));
        Assert.Equal("http://localhost/", Navigation.Uri);
    }

    [Fact]
    public void AnExplicitPageTheUserCanOpen_IsNotRedirected()
    {
        var cut = RenderGuard<WorkflowInstancesPage>(new StubPermissionService("workflows/*:view", "secrets:view"));

        Assert.Contains(PageContent, cut.Markup);
        Assert.Equal($"http://localhost/{InstancesPath}", Navigation.Uri);
    }

    [Fact]
    public void TheLandingPage_SkipsMenuItemsWithoutAnAppRelativeHref()
    {
        // Listed ahead of Secrets in its group, but neither has a page to open.
        UseMenu(
            new MenuItem { Text = "Header", GroupName = "administration", Order = -2 },
            new MenuItem { Text = "Docs", Href = "https://example.com/docs", GroupName = "administration", Order = -1 });

        var cut = RenderGuard<DashboardPage>(new StubPermissionService("secrets:view"), "/");

        cut.WaitForAssertion(() => Assert.Equal("http://localhost/security/secrets", Navigation.Uri));
    }

    [Fact]
    public void TheLandingPage_SkipsMenuItemsThatResolveToTheAppRoot()
    {
        UseMenu(
            new MenuItem { Text = "Hash", Href = "#", GroupName = "administration", Order = -3 },
            new MenuItem { Text = "Query", Href = "?x", GroupName = "administration", Order = -2 },
            new MenuItem { Text = "Hash root", Href = "#/", GroupName = "administration", Order = -1 });

        var cut = RenderGuard<DashboardPage>(new StubPermissionService("secrets:view"), "/");

        cut.WaitForAssertion(() => Assert.Equal("http://localhost/security/secrets", Navigation.Uri));
    }

    [Fact]
    public async Task TheLandingPage_DoesNotRedirect_WhenTheUserNavigatesAwayWhileTheMenuLoads()
    {
        var menuLoaded = new TaskCompletionSource();
        _menuHold = menuLoaded.Task;
        var cut = RenderGuard<DashboardPage>(new StubPermissionService("secrets:view"), "/");

        Navigation.NavigateTo(InstancesPath);
        menuLoaded.SetResult();
        await cut.InvokeAsync(() => { });

        Assert.Equal($"http://localhost/{InstancesPath}", Navigation.Uri);
    }

    [Fact]
    public async Task TheLandingPage_DoesNotRedirect_WhenTheUserIsGrantedItWhileTheMenuLoads()
    {
        var menuLoaded = new TaskCompletionSource();
        _menuHold = menuLoaded.Task;
        var permissions = new StubPermissionService("secrets:view");
        var authentication = new NotifyingAuthenticationStateProvider();
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        var cut = RenderGuard<DashboardPage>(permissions, "/");

        permissions.Permissions = StubPermissionService.Grants("secrets:view", "dashboard:view");
        authentication.Notify();
        cut.WaitForAssertion(() => Assert.Contains(PageContent, cut.Markup));
        menuLoaded.SetResult();
        await cut.InvokeAsync(() => { });

        Assert.Equal("http://localhost/", Navigation.Uri);
    }

    [Fact]
    public void TheLandingPage_ExplainsThatNoPagesAreAvailable_WhenTheMenuRecoversAfterAFailure()
    {
        var permissions = new StubPermissionService("secrets:view");
        var authentication = new NotifyingAuthenticationStateProvider();
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        Services.AddSingleton<IMenuService>(new UnavailableMenuService(failures: 1));
        var cut = RenderGuard<DashboardPage>(permissions, "/");
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='access-denied']")));

        permissions.Permissions = StubPermissionService.Grants("unrelated:view");
        authentication.Notify();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='no-accessible-pages']")));
        Assert.Empty(cut.FindAll("[data-testid='access-denied']"));
    }

    [Fact]
    public void TheLandingPage_StillRedirects_WhenTheUrlCarriesAQueryString()
    {
        var cut = RenderGuard<DashboardPage>(new StubPermissionService("secrets:view"), "/?tab=1");

        cut.WaitForAssertion(() => Assert.Equal("http://localhost/security/secrets", Navigation.Uri));
    }

    [Fact]
    public void TheLandingPage_RedirectsRelativeToTheBaseUri_WhenHostedUnderASubPath()
    {
        var navigation = new SubPathNavigationManager();
        Services.AddSingleton<NavigationManager>(navigation);

        var cut = RenderGuard<DashboardPage>(new StubPermissionService("secrets:view"), "http://localhost/studio/");

        cut.WaitForAssertion(() => Assert.Equal("http://localhost/studio/security/secrets", navigation.Uri));
        Assert.Empty(cut.FindAll("[data-testid='access-denied']"));
    }

    [Fact]
    public void TheLandingPage_StaysInsideTheApp_WhenTheFirstMenuHrefStartsWithASlash()
    {
        var navigation = new SubPathNavigationManager();
        Services.AddSingleton<NavigationManager>(navigation);
        UseMenu(new MenuItem { Text = "Rooted", Href = "/security/rooted", GroupName = "administration", Order = -1 });

        var cut = RenderGuard<DashboardPage>(new StubPermissionService("secrets:view"), "http://localhost/studio/");

        cut.WaitForAssertion(() => Assert.Equal("http://localhost/studio/security/rooted", navigation.Uri));
    }

    [Fact]
    public void AnExplicitPage_IsNotRedirected_EvenWhenTheLandingPageIsDenied()
    {
        var cut = RenderGuard<DashboardPage>(new StubPermissionService("secrets:view"), InstancesPath);

        Assert.NotEmpty(cut.FindAll("[data-testid='access-denied']"));
        Assert.Equal($"http://localhost/{InstancesPath}", Navigation.Uri);
    }

    [Fact]
    public void TheLandingPage_ShowsAccessDenied_WhenTheMenuCannotBeResolved()
    {
        Services.AddSingleton<IMenuService>(new UnavailableMenuService());

        var cut = RenderGuard<DashboardPage>(new StubPermissionService("secrets:view"), "/");

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='access-denied']")));
        Assert.Empty(cut.FindAll("[data-testid='no-accessible-pages']"));
        Assert.Equal("http://localhost/", Navigation.Uri);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    // Starts on a page other than the landing page by default, since only the landing page redirects.
    private IRenderedComponent<PermissionPageGuard> RenderGuard<TPage>(IPermissionService permissionService, string startPath = InstancesPath)
    {
        UsePermissions(permissionService, startPath);

        return Render<PermissionPageGuard>(parameters => parameters
            .AddCascadingValue(Route<TPage>())
            .AddChildContent(PageContent));
    }

    private void UsePermissions(IPermissionService permissionService, string startPath)
    {
        Services.AddSingleton(permissionService);
        Navigation.NavigateTo(startPath);
    }

    private void UseMenu(params MenuItem[] extraItems)
    {
        var menu = new LandingMenu(extraItems, () => _menuHold);
        Services.AddSingleton<IMenuService>(sp => new DefaultMenuService([menu], [menu], sp.GetService<IPermissionService>()));
    }

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    // CascadingValue takes a complete parameter set, so every render passes the guard along with the route.
    private static void RouteTo<TPage>(ComponentParameterCollectionBuilder<CascadingValue<RouteData>> parameters) => parameters
        .Add(x => x.Value, Route<TPage>())
        .AddChildContent<PermissionPageGuard>(guard => guard.AddChildContent(PageContent));

    private static RouteData Route<TPage>() => new(typeof(TPage), new Dictionary<string, object?>());

    // Fails the first `failures` times the menu is requested, then has nothing to show.
    private sealed class UnavailableMenuService(int failures = int.MaxValue) : IMenuService
    {
        private int _calls;

        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default) => new(Enumerable.Empty<MenuItem>());

        public ValueTask<IEnumerable<MenuItemGroup>> GetMenuItemGroupsAsync(CancellationToken cancellationToken = default) =>
            _calls++ < failures ? throw new InvalidOperationException("menu unavailable") : new(Enumerable.Empty<MenuItemGroup>());
    }

    // An application hosted under /studio/, which bUnit's fake navigation manager (always rooted at /) cannot model.
    private sealed class SubPathNavigationManager : NavigationManager
    {
        public SubPathNavigationManager() => Initialize("http://localhost/studio/", "http://localhost/studio/");

        protected override void NavigateToCore(string uri, NavigationOptions options) => Uri = ToAbsoluteUri(uri).ToString();
    }

    private sealed class UngatedPage : ComponentBase;

    [RequirePermission("dashboard", PermissionVerbs.View)]
    private sealed class DashboardPage : ComponentBase;

    // Navigation shaped like the built-in modules': Secrets is listed first but belongs to a later group.
    private sealed class LandingMenu(MenuItem[] extraItems, Func<Task?> hold) : IMenuProvider, IMenuGroupProvider
    {
        public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
        {
            if (hold() is { } pending)
                await pending;

            return new[]
            {
                Item("Secrets", "security/secrets", "administration", 0, new Permission("secrets", PermissionVerbs.View)),
                Item("Workflows", "", "general", 10, subMenuItems: [Item("Definitions", "workflows/definitions", "general", 0, new Permission("workflows/definitions", PermissionVerbs.View))]),
                Item("Dashboard", "", "general", 0, new Permission("dashboard", PermissionVerbs.View))
            }.Concat(extraItems);
        }

        public ValueTask<IEnumerable<MenuItemGroup>> GetMenuGroupsAsync(CancellationToken cancellationToken = default) => new(new[]
        {
            new MenuItemGroup("administration", "Administration", 1000f),
            new MenuItemGroup("general", "General")
        });

        private static MenuItem Item(string text, string href, string group, float order, Permission? permission = null, MenuItem[]? subMenuItems = null) => new()
        {
            Text = text, Href = href, GroupName = group, Order = order,
            RequiredPermissions = permission is { } required ? [required] : [],
            SubMenuItems = subMenuItems ?? []
        };
    }

    private sealed class PermissionsConsumer : ComponentBase
    {
        [CascadingParameter] public UserPermissions? Permissions { get; set; }
    }

    [RequirePermission("workflows/instances", PermissionVerbs.View)]
    [RequirePermission("workflows/definitions", PermissionVerbs.View)]
    private sealed class WorkflowInstancesPage : ComponentBase;
}
