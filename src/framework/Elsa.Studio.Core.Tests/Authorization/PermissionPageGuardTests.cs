using Bunit;
using Elsa.Studio.Authorization;
using Elsa.Studio.Components;
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
    public void AnUngatedPage_RendersWithoutResolvingPermissions()
    {
        var permissions = new StubPermissionService("secrets:view");

        var cut = RenderGuard<UngatedPage>(permissions);

        Assert.Contains(PageContent, cut.Markup);
        Assert.Equal(0, permissions.Calls);
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
            .AddCascadingValue(new RouteData(typeof(TPage), new Dictionary<string, object?>()))
            .AddChildContent(PageContent));
    }

    private sealed class UngatedPage : ComponentBase;

    [RequirePermission("workflows/instances", PermissionVerbs.View)]
    [RequirePermission("workflows/definitions", PermissionVerbs.View)]
    private sealed class WorkflowInstancesPage : ComponentBase;
}
