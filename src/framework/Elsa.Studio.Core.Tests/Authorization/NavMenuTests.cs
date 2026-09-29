using Bunit;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
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
        Services.AddSingleton<IMenuService>(_menu);
        Services.AddSingleton<AuthenticationStateProvider>(_authentication);
    }

    [Fact]
    public void TheMenuIsRebuiltWhenTheAuthenticationStateChanges()
    {
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
}
