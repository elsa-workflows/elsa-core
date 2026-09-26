using Bunit;
using Elsa.Studio.Security.Components;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Menu;
using Elsa.Studio.Security.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Security.Tests;

public sealed class SecurityMenuTests
{
    private static readonly UserAdministrationAccess ViewableUsers =
        new(UserAdministrationAccessState.Ready, CanView: true, CanCreate: false, CanUpdate: false, CanDelete: false);

    private static readonly RoleAdministrationAccess ViewableRoles =
        new(RoleAdministrationAccessState.Ready, CanView: true, CanCreate: true, CanUpdate: false, CanDelete: false);

    [Fact]
    public async Task GetMenuItemsAsync_WhenBothAccessesAreForbidden_HidesTheWholeGroup()
    {
        var menu = CreateMenu(featureEnabled: true, UserAdministrationAccess.Forbidden, RoleAdministrationAccess.Forbidden);

        Assert.Empty(await menu.GetMenuItemsAsync());
    }

    [Fact]
    public async Task GetMenuItemsAsync_WhenTheIdentityFeatureIsDisabled_HidesTheGroupWithoutConsultingPermissions()
    {
        var users = new TestUserAccessService(ViewableUsers);
        var roles = new TestRoleAccessService(ViewableRoles);
        var menu = new SecurityMenu([new IdentitySecurityMenuContributor(new TestRemoteFeatureProvider(false), users, roles)]);

        Assert.Empty(await menu.GetMenuItemsAsync());
        Assert.Equal(0, users.Calls);
        Assert.Equal(0, roles.Calls);
    }

    [Fact]
    public async Task GetMenuItemsAsync_ListsUsersBeforeRoles()
    {
        var menu = CreateMenu(featureEnabled: true, ViewableUsers, ViewableRoles);

        var group = Assert.Single(await menu.GetMenuItemsAsync());

        Assert.Equal("Identity & access", group.Text);
        Assert.Equal("security/users", group.Href);
        Assert.Collection(group.SubMenuItems,
            users =>
            {
                Assert.Equal("Users", users.Text);
                Assert.Equal("security/users", users.Href);
                Assert.Equal(10, users.Order);
            },
            roles =>
            {
                Assert.Equal("Roles", roles.Text);
                Assert.Equal("security/roles", roles.Href);
                Assert.Equal(20, roles.Order);
            });
    }

    [Fact]
    public async Task GetMenuItemsAsync_ShowsUsersEvenWhenRolesIsForbidden()
    {
        var menu = CreateMenu(featureEnabled: true, ViewableUsers, RoleAdministrationAccess.Forbidden);

        var group = Assert.Single(await menu.GetMenuItemsAsync());

        var item = Assert.Single(group.SubMenuItems);
        Assert.Equal("Users", item.Text);
        Assert.Equal("security/users", group.Href);
    }

    [Fact]
    public async Task GetMenuItemsAsync_ShowsRolesEvenWhenUsersIsUnavailable()
    {
        var menu = CreateMenu(featureEnabled: true, UserAdministrationAccess.Unavailable, ViewableRoles);

        var group = Assert.Single(await menu.GetMenuItemsAsync());

        var item = Assert.Single(group.SubMenuItems);
        Assert.Equal("Roles", item.Text);
        Assert.Equal("security/roles", group.Href);
    }

    private static SecurityMenu CreateMenu(bool featureEnabled, UserAdministrationAccess userAccess, RoleAdministrationAccess roleAccess) =>
        new([new IdentitySecurityMenuContributor(
            new TestRemoteFeatureProvider(featureEnabled),
            new TestUserAccessService(userAccess),
            new TestRoleAccessService(roleAccess))]);
}

public sealed class RoleAdministrationAccessBoundaryTests : BunitContext, IAsyncLifetime
{
    public RoleAdministrationAccessBoundaryTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public void Render_WhenAccessIsForbidden_ShowsThePermissionRequiredState()
    {
        Services.AddSingleton<IRoleAdministrationAccessService>(new TestRoleAccessService(RoleAdministrationAccess.Forbidden));

        var cut = Render<RoleAdministrationAccessBoundary>(parameters =>
            parameters.Add(component => component.ChildContent, Child("ready")));

        cut.WaitForAssertion(() => Assert.Contains("Role administration access is required", cut.Markup));
        Assert.DoesNotContain("ready", cut.Markup);
    }

    [Fact]
    public void Render_WhenAccessIsUnavailable_ShowsTheUnavailableState()
    {
        Services.AddSingleton<IRoleAdministrationAccessService>(new TestRoleAccessService(RoleAdministrationAccess.Unavailable));

        var cut = Render<RoleAdministrationAccessBoundary>(parameters =>
            parameters.Add(component => component.ChildContent, Child("ready")));

        cut.WaitForAssertion(() => Assert.Contains("Role administration is unavailable", cut.Markup));
        Assert.DoesNotContain("ready", cut.Markup);
    }

    [Fact]
    public void Render_WhenAccessIsReady_RendersTheAuthorizedChildContent()
    {
        Services.AddSingleton<IRoleAdministrationAccessService>(new TestRoleAccessService(new RoleAdministrationAccess(
            RoleAdministrationAccessState.Ready, CanView: true, CanCreate: false, CanUpdate: false, CanDelete: false)));

        var cut = Render<RoleAdministrationAccessBoundary>(parameters =>
            parameters.Add(component => component.ChildContent, Child("authorized")));

        cut.WaitForAssertion(() => Assert.Contains("authorized", cut.Markup));
        Assert.DoesNotContain("Role administration access is required", cut.Markup);
        Assert.DoesNotContain("Role administration is unavailable", cut.Markup);
    }

    [Fact]
    public async Task Dispose_CancelsAnOutstandingAccessCheck()
    {
        var service = new BlockingRoleAccessService();
        Services.AddSingleton<IRoleAdministrationAccessService>(service);

        var cut = Render<RoleAdministrationAccessBoundary>(parameters =>
            parameters.Add(component => component.ChildContent, Child("ready")));
        await service.Started.Task;

        await Assert.IsAssignableFrom<IAsyncDisposable>(cut.Instance).DisposeAsync();

        Assert.True(service.CancellationToken.IsCancellationRequested);
        service.Release.TrySetResult(RoleAdministrationAccess.Unavailable);
    }

    private static RenderFragment<RoleAdministrationAccess> Child(string text) =>
        access => builder => builder.AddContent(0, $"{text}:{access.CanView}");

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();
}

public sealed class UserAdministrationAccessBoundaryTests : BunitContext, IAsyncLifetime
{
    public UserAdministrationAccessBoundaryTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public void Render_WhenAccessIsForbidden_ShowsThePermissionRequiredState()
    {
        Services.AddSingleton<IUserAdministrationAccessService>(new TestUserAccessService(UserAdministrationAccess.Forbidden));

        var cut = Render<UserAdministrationAccessBoundary>(parameters =>
            parameters.Add(component => component.ChildContent, Child("ready")));

        cut.WaitForAssertion(() => Assert.Contains("User administration access is required", cut.Markup));
        Assert.Contains("identity/users:view", cut.Markup);
        Assert.DoesNotContain("ready", cut.Markup);
    }

    [Fact]
    public void Render_WhenAccessIsUnavailable_ShowsTheUnavailableStateWithRetry()
    {
        var service = new TestUserAccessService(UserAdministrationAccess.Unavailable);
        Services.AddSingleton<IUserAdministrationAccessService>(service);

        var cut = Render<UserAdministrationAccessBoundary>(parameters =>
            parameters.Add(component => component.ChildContent, Child("ready")));

        cut.WaitForAssertion(() => Assert.Contains("User administration is unavailable", cut.Markup));
        Assert.DoesNotContain("ready", cut.Markup);

        service.Access = new UserAdministrationAccess(UserAdministrationAccessState.Ready, CanView: true, CanCreate: false, CanUpdate: false, CanDelete: false);
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Try again").Click();

        cut.WaitForAssertion(() => Assert.Contains("ready:True", cut.Markup));
        Assert.Equal(1, service.Invalidations);
    }

    [Fact]
    public void Render_WhenAccessIsReady_RendersTheAuthorizedChildContent()
    {
        Services.AddSingleton<IUserAdministrationAccessService>(new TestUserAccessService(new UserAdministrationAccess(
            UserAdministrationAccessState.Ready, CanView: true, CanCreate: false, CanUpdate: false, CanDelete: false)));

        var cut = Render<UserAdministrationAccessBoundary>(parameters =>
            parameters.Add(component => component.ChildContent, Child("authorized")));

        cut.WaitForAssertion(() => Assert.Contains("authorized:True", cut.Markup));
        Assert.DoesNotContain("User administration access is required", cut.Markup);
        Assert.DoesNotContain("User administration is unavailable", cut.Markup);
    }

    [Fact]
    public async Task Dispose_CancelsAnOutstandingAccessCheck()
    {
        var service = new BlockingUserAccessService();
        Services.AddSingleton<IUserAdministrationAccessService>(service);

        var cut = Render<UserAdministrationAccessBoundary>(parameters =>
            parameters.Add(component => component.ChildContent, Child("ready")));
        await service.Started.Task;

        await Assert.IsAssignableFrom<IAsyncDisposable>(cut.Instance).DisposeAsync();

        Assert.True(service.CancellationToken.IsCancellationRequested);
        service.Release.TrySetResult(UserAdministrationAccess.Unavailable);
    }

    private static RenderFragment<UserAdministrationAccess> Child(string text) =>
        access => builder => builder.AddContent(0, $"{text}:{access.CanView}");

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();
}

internal sealed class TestRoleAccessService(RoleAdministrationAccess access) : IRoleAdministrationAccessService
{
    public int Calls { get; private set; }

    public Task<RoleAdministrationAccess> GetAsync(CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(access);
    }

    public void Invalidate()
    {
    }
}

internal sealed class TestUserAccessService(UserAdministrationAccess access) : IUserAdministrationAccessService
{
    public UserAdministrationAccess Access { get; set; } = access;
    public int Calls { get; private set; }
    public int Invalidations { get; private set; }

    public Task<UserAdministrationAccess> GetAsync(CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(Access);
    }

    public void Invalidate() => Invalidations++;
}

internal abstract class BlockingAccessService<TAccess>
{
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<TAccess> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public CancellationToken CancellationToken { get; private set; }

    public async Task<TAccess> GetAsync(CancellationToken cancellationToken = default)
    {
        CancellationToken = cancellationToken;
        Started.TrySetResult();
        return await Release.Task.WaitAsync(cancellationToken);
    }

    public void Invalidate()
    {
    }
}

internal sealed class BlockingRoleAccessService : BlockingAccessService<RoleAdministrationAccess>, IRoleAdministrationAccessService;

internal sealed class BlockingUserAccessService : BlockingAccessService<UserAdministrationAccess>, IUserAdministrationAccessService;
