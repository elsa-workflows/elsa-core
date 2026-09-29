using Bunit;
using Elsa.Studio.Authorization;
using Elsa.Studio.Components;
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

    public PermissionPageGuardTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
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
        Services.AddSingleton<IPermissionService>(permissions);
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

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    private IRenderedComponent<PermissionPageGuard> RenderGuard<TPage>(IPermissionService permissionService)
    {
        Services.AddSingleton(permissionService);

        return Render<PermissionPageGuard>(parameters => parameters
            .AddCascadingValue(Route<TPage>())
            .AddChildContent(PageContent));
    }

    // CascadingValue takes a complete parameter set, so every render passes the guard along with the route.
    private static void RouteTo<TPage>(ComponentParameterCollectionBuilder<CascadingValue<RouteData>> parameters) => parameters
        .Add(x => x.Value, Route<TPage>())
        .AddChildContent<PermissionPageGuard>(guard => guard.AddChildContent(PageContent));

    private static RouteData Route<TPage>() => new(typeof(TPage), new Dictionary<string, object?>());

    private sealed class UngatedPage : ComponentBase;

    private sealed class PermissionsConsumer : ComponentBase
    {
        [CascadingParameter] public UserPermissions? Permissions { get; set; }
    }

    [RequirePermission("workflows/instances", PermissionVerbs.View)]
    [RequirePermission("workflows/definitions", PermissionVerbs.View)]
    private sealed class WorkflowInstancesPage : ComponentBase;
}
