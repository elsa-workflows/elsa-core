using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Labels.Menu;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Secrets.Menu;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Menu;
using Elsa.Studio.Security.Models;
using Elsa.Studio.Services;
using Microsoft.Extensions.Localization;
using MudBlazor;
using Xunit;

namespace Elsa.Studio.Administration.Tests;

public class AdministrationNavigationTests
{
    [Fact]
    public async Task DefaultMenuGroupProvider_UsesAdministrationAndRetainsSettingsAlias()
    {
        var groups = (await new DefaultMenuGroupProvider().GetMenuGroupsAsync()).ToList();

        var administration = Assert.Single(groups, x => x.Name == MenuItemGroups.Administration.Name);
        Assert.Equal("Administration", administration.Text);
        Assert.Equal("security", administration.Name);
        var legacySettings = typeof(MenuItemGroups).GetField("Settings");
        Assert.NotNull(legacySettings);
        Assert.Same(MenuItemGroups.Administration, legacySettings.GetValue(null));
    }

    [Fact]
    public async Task LabelsMenu_PlacesLabelsInAdministration()
    {
        var item = Assert.Single(await new LabelsMenu(new TestLocalizer(), new EnabledRemoteFeatureProvider()).GetMenuItemsAsync());

        Assert.Equal(MenuItemGroups.Administration.Name, item.GroupName);
        Assert.Equal(200, item.Order);
    }

    [Fact]
    public async Task SecretsMenu_PlacesSecretsInAdministration()
    {
        var item = Assert.Single(await new SecretsMenu(new EnabledRemoteFeatureProvider()).GetMenuItemsAsync());

        Assert.Equal(MenuItemGroups.Administration.Name, item.GroupName);
        Assert.Equal(300, item.Order);
    }

    [Fact]
    public async Task SecurityMenu_ExposesIdentityAndAccessInAdministration()
    {
        var item = Assert.Single(await new SecurityMenu([new StaticSecurityMenuContributor()]).GetMenuItemsAsync());

        Assert.Equal("Identity & access", item.Text);
        Assert.Equal(Icons.Material.Filled.ManageAccounts, item.Icon);
        Assert.Equal(MenuItemGroups.Administration.Name, item.GroupName);
        Assert.Equal(100, item.Order);
    }

    [Fact]
    public async Task IdentityMenu_ListsUsersBeforeRolesUsingStructuredAccess()
    {
        Assert.Equal("Elsa.Identity.ShellFeatures.Identity", Elsa.Studio.Security.Feature.RemoteFeatureName);

        var contributor = new IdentitySecurityMenuContributor(
            new EnabledRemoteFeatureProvider(),
            new TestUserAdministrationAccessService(canView: true),
            new TestRoleAdministrationAccessService(canView: true));

        var items = (await contributor.GetMenuItemsAsync()).ToList();

        Assert.Collection(items,
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

    [Theory]
    [InlineData(true, false, "Users")]
    [InlineData(false, true, "Roles")]
    public async Task IdentityMenu_GatesUsersAndRolesIndependently(bool canViewUsers, bool canViewRoles, string expected)
    {
        var contributor = new IdentitySecurityMenuContributor(
            new EnabledRemoteFeatureProvider(),
            new TestUserAdministrationAccessService(canViewUsers),
            new TestRoleAdministrationAccessService(canViewRoles));

        var item = Assert.Single(await contributor.GetMenuItemsAsync());

        Assert.Equal(expected, item.Text);
    }

    [Fact]
    public async Task IdentityMenu_HidesTheGroupWhenNoChildIsViewable()
    {
        var contributor = new IdentitySecurityMenuContributor(
            new EnabledRemoteFeatureProvider(),
            new TestUserAdministrationAccessService(canView: false),
            new TestRoleAdministrationAccessService(canView: false));

        Assert.Empty(await new SecurityMenu([contributor]).GetMenuItemsAsync());
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }

    private sealed class EnabledRemoteFeatureProvider : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
    }

    private sealed class StaticSecurityMenuContributor : ISecurityMenuContributor
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default) => new([
            new MenuItem { Href = "security/test", Text = "Test" }
        ]);
    }

    private sealed class TestRoleAdministrationAccessService(bool canView) : IRoleAdministrationAccessService
    {
        public Task<RoleAdministrationAccess> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(canView
                ? new RoleAdministrationAccess(RoleAdministrationAccessState.Ready, CanView: true, CanCreate: false, CanUpdate: false, CanDelete: false)
                : RoleAdministrationAccess.Forbidden);

        public void Invalidate()
        {
        }
    }

    private sealed class TestUserAdministrationAccessService(bool canView) : IUserAdministrationAccessService
    {
        public Task<UserAdministrationAccess> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(canView
                ? new UserAdministrationAccess(UserAdministrationAccessState.Ready, CanView: true, CanCreate: false, CanUpdate: false, CanDelete: false)
                : UserAdministrationAccess.Forbidden);

        public void Invalidate()
        {
        }
    }
}
