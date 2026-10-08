using Elsa.Studio.Authorization;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Elsa.Studio.Testing;
using Xunit;

namespace Elsa.Studio.Core.Tests.Authorization;

/// <summary>
/// Uses a navigation shaped like the built-in modules' so the scenarios read like the reported one: a user whose
/// only role grants Secrets permissions.
/// </summary>
public class DefaultMenuServiceTests
{
    private readonly MenuItem _dashboard = Item("Dashboard", "", new Permission("dashboard", "view"));
    private readonly MenuItem _definitions = Item("Definitions", "workflows/definitions", new Permission("workflows/definitions", "view"));
    private readonly MenuItem _instances = Item("Instances", "workflows/instances", new Permission("workflows/instances", "view"));
    private readonly MenuItem _secrets = Item("Secrets", "security/secrets", new Permission("secrets", "view"));
    private readonly MenuItem _webhooks = Item("Webhooks", "webhooks");
    private readonly MenuItem _workflows;

    public DefaultMenuServiceTests()
    {
        _workflows = Item("Workflows", "");
        _workflows.SubMenuItems.Add(_definitions);
        _workflows.SubMenuItems.Add(_instances);
    }

    [Fact]
    public async Task ASecretsOnlyUser_SeesOnlySecretsAndUngatedItems()
    {
        var items = await GetMenuItemsAsync(new StubPermissionService("secrets:view", "secrets:write", "secrets:delete"));

        Assert.Equal(["Secrets", "Webhooks"], items.Select(x => x.Text).Order());
    }

    [Fact]
    public async Task AParentWhoseChildrenAreAllHidden_IsHidden()
    {
        var items = await GetMenuItemsAsync(new StubPermissionService("dashboard:view"));

        Assert.DoesNotContain(items, x => x.Text == "Workflows");
    }

    [Fact]
    public async Task AParent_KeepsOnlyItsPermittedChildren()
    {
        var items = await GetMenuItemsAsync(new StubPermissionService("workflows/instances:view"));

        var workflows = Assert.Single(items, x => x.Text == "Workflows");
        Assert.Equal(["Instances"], workflows.SubMenuItems.Select(x => x.Text));
    }

    [Fact]
    public async Task AParentsOwnRequirement_HidesItRegardlessOfItsChildren()
    {
        _workflows.RequiredPermissions.Add(new("system/features", "view"));

        var items = await GetMenuItemsAsync(new StubPermissionService("workflows/*:view"));

        Assert.DoesNotContain(items, x => x.Text == "Workflows");
    }

    [Fact]
    public async Task HierarchicalWildcards_AreHonored()
    {
        var items = await GetMenuItemsAsync(new StubPermissionService("workflows/*:view"));

        var workflows = Assert.Single(items, x => x.Text == "Workflows");
        Assert.Equal(["Definitions", "Instances"], workflows.SubMenuItems.Select(x => x.Text));
    }

    [Fact]
    public async Task TheWildcardGrant_SeesEverything()
    {
        var items = await GetMenuItemsAsync(new StubPermissionService("*"));

        Assert.Equal(["Dashboard", "Secrets", "Webhooks", "Workflows"], items.Select(x => x.Text).Order());
        Assert.Equal(2, Assert.Single(items, x => x.Text == "Workflows").SubMenuItems.Count);
    }

    [Fact]
    public async Task WhenPermissionsAreUnknown_EverythingIsShownAsBefore()
    {
        var items = await GetMenuItemsAsync(new StubPermissionService(UserPermissions.Unknown));

        Assert.Equal(["Dashboard", "Secrets", "Webhooks", "Workflows"], items.Select(x => x.Text).Order());
    }

    [Fact]
    public async Task WithoutAPermissionService_EverythingIsShownAsBefore()
    {
        var service = new DefaultMenuService([new StaticMenuProvider(_dashboard, _workflows)], []);

        var items = (await service.GetMenuItemsAsync()).ToList();

        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task Filtering_DoesNotMutateTheProvidersItems()
    {
        await GetMenuItemsAsync(new StubPermissionService("workflows/instances:view"));

        Assert.Equal(2, _workflows.SubMenuItems.Count);
    }

    private async Task<IReadOnlyList<MenuItem>> GetMenuItemsAsync(IPermissionService permissionService)
    {
        var service = new DefaultMenuService([new StaticMenuProvider(_dashboard, _workflows, _secrets, _webhooks)], [], permissionService);
        return (await service.GetMenuItemsAsync()).ToList();
    }

    private static MenuItem Item(string text, string href, params Permission[] requiredPermissions) =>
        new() { Text = text, Href = href, RequiredPermissions = requiredPermissions.ToList() };

    private sealed class StaticMenuProvider(params MenuItem[] items) : IMenuProvider
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default) => new(items);
    }
}
