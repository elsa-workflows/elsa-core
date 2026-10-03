using Bunit;
using Elsa.Studio.Authorization;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Elsa.Studio.Testing;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Core.Tests.Authorization;

/// <summary>
/// The menu is filtered by the user's permissions, so it must be rebuilt when the authentication state changes rather
/// than keep the items it loaded when the shell started.
/// </summary>
public sealed class NavMenuTests : BunitContext, IAsyncLifetime
{
    private readonly SwitchableMenuService _menu = new();
    private readonly NotifyingAuthenticationStateProvider _authentication = new();

    public NavMenuTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<AuthenticationStateProvider>(_authentication);
    }

    [Fact]
    public void TheMenuIsRebuiltWhenTheAuthenticationStateChanges()
    {
        Services.AddSingleton<IMenuService>(_menu);
        _menu.Items = [Item("Workflows")];
        var cut = Render<NavMenu>();
        cut.WaitForAssertion(() => Assert.Contains("Workflows", cut.Markup));

        _menu.Items = [Item("Secrets")];
        _authentication.Notify();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Secrets", cut.Markup);
            Assert.DoesNotContain("Workflows", cut.Markup);
        });
    }

    [Fact]
    public void TheMenuIsRebuiltWhenThePermissionSnapshotChanges()
    {
        var permissions = new StubPermissionService("secrets:view");
        var cache = new TestPermissionSnapshotCache();
        Services.AddSingleton<IPermissionService>(permissions);
        Services.AddSingleton<IPermissionSnapshotCache>(cache);
        Services.AddSingleton<IMenuService>(new DefaultMenuService(
            [new StaticMenuProvider(new MenuItem
            {
                Text = "Secrets",
                Href = "security/secrets",
                GroupName = "general",
                RequiredPermissions = [new Permission("secrets", PermissionVerbs.View)]
            })],
            [new StaticMenuGroupProvider(new MenuItemGroup("general", "General"))],
            permissions));

        var cut = Render<NavMenu>();
        cut.WaitForAssertion(() => Assert.Contains("Secrets", cut.Markup));

        permissions.Permissions = StubPermissionService.Grants();
        cache.Invalidate();

        cut.WaitForAssertion(() => Assert.DoesNotContain("Secrets", cut.Markup));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    private static MenuItem Item(string text) => new() { Text = text, Href = text.ToLowerInvariant(), GroupName = "general" };

    private sealed class SwitchableMenuService : IMenuService
    {
        public IReadOnlyList<MenuItem> Items { get; set; } = [];

        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default) => new(Items);

        public ValueTask<IEnumerable<MenuItemGroup>> GetMenuItemGroupsAsync(CancellationToken cancellationToken = default) =>
            new([new MenuItemGroup("general", "General")]);
    }

    private sealed class StaticMenuProvider(MenuItem item) : IMenuProvider
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default) => new([item]);
    }

    private sealed class StaticMenuGroupProvider(MenuItemGroup group) : IMenuGroupProvider
    {
        public ValueTask<IEnumerable<MenuItemGroup>> GetMenuGroupsAsync(CancellationToken cancellationToken = default) => new([group]);
    }

    private sealed class TestPermissionSnapshotCache : IPermissionSnapshotCache
    {
        public event EventHandler? Changed;

        public void Invalidate() => Changed?.Invoke(this, EventArgs.Empty);
    }
}
