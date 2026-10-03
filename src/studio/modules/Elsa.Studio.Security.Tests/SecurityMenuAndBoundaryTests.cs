using Bunit;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Components;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Menu;
using Elsa.Studio.Security.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
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
    public void Render_WhenAccessIsForbidden_ShowsTheSharedAccessDeniedState()
    {
        Services.AddSingleton<IRoleAdministrationAccessService>(new TestRoleAccessService(RoleAdministrationAccess.Forbidden));

        var cut = Render<RoleAdministrationAccessBoundary>(parameters =>
            parameters.Add(component => component.ChildContent, Child("ready")));

        cut.WaitForAssertion(() => Assert.Contains("identity/roles:view", cut.FindComponent<AccessDenied>().Markup));
        Assert.DoesNotContain("ready", cut.Markup);
    }

    [Fact]
    public void Render_WhenAccessIsUnavailable_ShowsTheUnavailableState()
    {
        Services.AddSingleton<IRoleAdministrationAccessService>(new TestRoleAccessService(RoleAdministrationAccess.Unavailable));

        var cut = Render<RoleAdministrationAccessBoundary>(parameters =>
            parameters.Add(component => component.ChildContent, Child("ready")));

        cut.WaitForAssertion(() => Assert.Contains("Role administration is unavailable", cut.Markup));
        Assert.Empty(cut.FindComponents<AccessDenied>());
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
        Assert.Empty(cut.FindComponents<AccessDenied>());
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

    [Fact]
    public void Render_WhenSnapshotChangesFromAdminToViewOnly_HidesMutations()
    {
        var service = new TestRoleAccessService(new RoleAdministrationAccess(
            RoleAdministrationAccessState.Ready, CanView: true, CanCreate: true, CanUpdate: true, CanDelete: true));
        var cache = new TestPermissionSnapshotCache();
        Services.AddSingleton<IRoleAdministrationAccessService>(service);
        Services.AddSingleton<IPermissionSnapshotCache>(cache);

        var cut = Render<RoleAdministrationAccessBoundary>(parameters =>
            parameters.Add(component => component.ChildContent, Mutations));

        cut.WaitForAssertion(() => Assert.Contains("create:True;update:True;delete:True", cut.Markup));

        service.Access = new RoleAdministrationAccess(
            RoleAdministrationAccessState.Ready, CanView: true, CanCreate: false, CanUpdate: false, CanDelete: false);
        cache.Invalidate();

        cut.WaitForAssertion(() => Assert.Contains("create:False;update:False;delete:False", cut.Markup));
    }

    [Fact]
    public async Task Render_WhenChangedArrivesDuringACheck_KeepsEditsAndHonoursTheQueuedRefresh()
    {
        var admin = new RoleAdministrationAccess(RoleAdministrationAccessState.Ready, CanView: true, CanCreate: true, CanUpdate: true, CanDelete: true);
        var viewOnly = new RoleAdministrationAccess(RoleAdministrationAccessState.Ready, CanView: true, CanCreate: false, CanUpdate: false, CanDelete: false);
        var service = new ControllableRoleAccessService(admin);
        var cache = new TestPermissionSnapshotCache();
        Services.AddSingleton<IRoleAdministrationAccessService>(service);
        Services.AddSingleton<IPermissionSnapshotCache>(cache);

        var cut = Render<RoleAdministrationAccessBoundary>(parameters =>
            parameters.Add(component => component.ChildContent, Editor));

        cut.WaitForAssertion(() => Assert.Contains("create:True", cut.Markup));
        cut.Find("#draft").Change("kept");
        Assert.Equal("kept", cut.Find("#draft").GetAttribute("value"));

        service.Block = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        service.Results.Enqueue(admin);
        service.Results.Enqueue(viewOnly);
        cache.Invalidate();
        await service.WaitForCallAsync(2);

        Assert.Contains("create:False", cut.Markup);
        Assert.DoesNotContain("Checking access", cut.Markup);
        Assert.Equal("kept", cut.Find("#draft").GetAttribute("value"));

        cache.Invalidate();
        service.Block.SetResult();

        cut.WaitForAssertion(() => Assert.Contains("create:False", cut.Markup));
        Assert.DoesNotContain("Checking access", cut.Markup);
        Assert.Equal(3, service.Calls);
    }

    private static RenderFragment<RoleAdministrationAccess> Child(string text) =>
        access => builder => builder.AddContent(0, $"{text}:{access.CanView}");

    private static RenderFragment<RoleAdministrationAccess> Mutations =>
        access => builder => builder.AddContent(0, $"create:{access.CanCreate};update:{access.CanUpdate};delete:{access.CanDelete}");

    private static RenderFragment<RoleAdministrationAccess> Editor =>
        access => builder =>
        {
            builder.OpenComponent<DraftEditor>(0);
            builder.AddAttribute(1, nameof(DraftEditor.AccessText), $"create:{access.CanCreate}");
            builder.CloseComponent();
        };

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
    public void Render_WhenAccessIsForbidden_ShowsTheSharedAccessDeniedState()
    {
        Services.AddSingleton<IUserAdministrationAccessService>(new TestUserAccessService(UserAdministrationAccess.Forbidden));

        var cut = Render<UserAdministrationAccessBoundary>(parameters =>
            parameters.Add(component => component.ChildContent, Child("ready")));

        cut.WaitForAssertion(() => Assert.Contains("identity/users:view", cut.FindComponent<AccessDenied>().Markup));
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
        Assert.Empty(cut.FindComponents<AccessDenied>());
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
        Assert.Empty(cut.FindComponents<AccessDenied>());
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

    [Fact]
    public void Render_WhenSnapshotChangesFromAdminToViewOnly_HidesMutations()
    {
        var service = new TestUserAccessService(new UserAdministrationAccess(
            UserAdministrationAccessState.Ready, CanView: true, CanCreate: true, CanUpdate: true, CanDelete: true));
        var cache = new TestPermissionSnapshotCache();
        Services.AddSingleton<IUserAdministrationAccessService>(service);
        Services.AddSingleton<IPermissionSnapshotCache>(cache);

        var cut = Render<UserAdministrationAccessBoundary>(parameters =>
            parameters.Add(component => component.ChildContent, Mutations));

        cut.WaitForAssertion(() => Assert.Contains("create:True;update:True;delete:True", cut.Markup));

        service.Access = new UserAdministrationAccess(
            UserAdministrationAccessState.Ready, CanView: true, CanCreate: false, CanUpdate: false, CanDelete: false);
        cache.Invalidate();

        cut.WaitForAssertion(() => Assert.Contains("create:False;update:False;delete:False", cut.Markup));
    }

    [Fact]
    public async Task Render_WhenChangedArrivesDuringACheck_KeepsEditsAndHonoursTheQueuedRefresh()
    {
        var admin = new UserAdministrationAccess(UserAdministrationAccessState.Ready, CanView: true, CanCreate: true, CanUpdate: true, CanDelete: true);
        var viewOnly = new UserAdministrationAccess(UserAdministrationAccessState.Ready, CanView: true, CanCreate: false, CanUpdate: false, CanDelete: false);
        var service = new ControllableUserAccessService(admin);
        var cache = new TestPermissionSnapshotCache();
        Services.AddSingleton<IUserAdministrationAccessService>(service);
        Services.AddSingleton<IPermissionSnapshotCache>(cache);

        var cut = Render<UserAdministrationAccessBoundary>(parameters =>
            parameters.Add(component => component.ChildContent, Editor));

        cut.WaitForAssertion(() => Assert.Contains("create:True", cut.Markup));
        cut.Find("#draft").Change("kept");
        Assert.Equal("kept", cut.Find("#draft").GetAttribute("value"));

        service.Block = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        service.Results.Enqueue(admin);
        service.Results.Enqueue(viewOnly);
        cache.Invalidate();
        await service.WaitForCallAsync(2);

        Assert.Contains("create:False", cut.Markup);
        Assert.DoesNotContain("Checking access", cut.Markup);
        Assert.Equal("kept", cut.Find("#draft").GetAttribute("value"));

        cache.Invalidate();
        service.Block.SetResult();

        cut.WaitForAssertion(() => Assert.Contains("create:False", cut.Markup));
        Assert.DoesNotContain("Checking access", cut.Markup);
        Assert.Equal(3, service.Calls);
    }

    private static RenderFragment<UserAdministrationAccess> Child(string text) =>
        access => builder => builder.AddContent(0, $"{text}:{access.CanView}");

    private static RenderFragment<UserAdministrationAccess> Mutations =>
        access => builder => builder.AddContent(0, $"create:{access.CanCreate};update:{access.CanUpdate};delete:{access.CanDelete}");

    private static RenderFragment<UserAdministrationAccess> Editor =>
        access => builder =>
        {
            builder.OpenComponent<DraftEditor>(0);
            builder.AddAttribute(1, nameof(DraftEditor.AccessText), $"create:{access.CanCreate}");
            builder.CloseComponent();
        };

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();
}

internal sealed class TestRoleAccessService(RoleAdministrationAccess access) : IRoleAdministrationAccessService
{
    public RoleAdministrationAccess Access { get; set; } = access;
    public int Calls { get; private set; }

    public Task<RoleAdministrationAccess> GetAsync(CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(Access);
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

internal abstract class ControllableAccessService<TAccess>(TAccess access)
{
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _calls;
    public TAccess Access { get; set; } = access;
    public Queue<TAccess> Results { get; } = new();
    public TaskCompletionSource? Block { get; set; }
    public int Calls => _calls;

    public async Task<TAccess> GetAsync(CancellationToken cancellationToken = default)
    {
        var call = Interlocked.Increment(ref _calls);
        if (call == 2)
            _started.TrySetResult();
        if (Block != null && call > 1)
            await Block.Task.WaitAsync(cancellationToken);
        return Results.Count > 0 ? Results.Dequeue() : Access;
    }

    public Task WaitForCallAsync(int call) =>
        call == 2 ? _started.Task.WaitAsync(TimeSpan.FromSeconds(5)) : Task.CompletedTask;

    public void Invalidate()
    {
    }
}

internal sealed class ControllableRoleAccessService(RoleAdministrationAccess access)
    : ControllableAccessService<RoleAdministrationAccess>(access), IRoleAdministrationAccessService;

internal sealed class ControllableUserAccessService(UserAdministrationAccess access)
    : ControllableAccessService<UserAdministrationAccess>(access), IUserAdministrationAccessService;

internal sealed class TestPermissionSnapshotCache : IPermissionSnapshotCache
{
    public event EventHandler? Changed;

    public void Invalidate() => Changed?.Invoke(this, EventArgs.Empty);
}

internal sealed class DraftEditor : ComponentBase
{
    [Parameter] public string AccessText { get; set; } = "";

    private string _draft = "";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddContent(1, AccessText);
        builder.OpenElement(2, "input");
        builder.AddAttribute(3, "id", "draft");
        builder.AddAttribute(4, "value", _draft);
        builder.AddAttribute(5, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, args => _draft = args.Value?.ToString() ?? ""));
        builder.CloseElement();
        builder.CloseElement();
    }
}
