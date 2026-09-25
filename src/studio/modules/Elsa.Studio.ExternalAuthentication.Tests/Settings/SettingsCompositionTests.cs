using Bunit;
using Elsa.Studio.Settings.Contracts;
using Elsa.Studio.Settings.Menu;
using Elsa.Studio.Settings.Models;
using Elsa.Studio.Settings.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using SettingsPage = Elsa.Studio.Settings.Pages.Index;

namespace Elsa.Studio.ExternalAuthentication.Tests.Settings;

public sealed class SettingsCompositionTests : BunitContext, IAsyncLifetime
{
    public SettingsCompositionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task SamePermissionFilteredDescriptorsDriveLandingAndSubmenuInDeterministicOrder()
    {
        var registry = new SettingsSectionRegistry(
        [
            new Provider(
            [
                Section("zulu", "Zulu", 10),
                Section("alpha", "Alpha", 10)
            ]),
            new Provider([])
        ]);
        Services.AddSingleton<ISettingsSectionRegistry>(registry);

        var ordered = await registry.ListAsync();
        Assert.Equal(["alpha", "zulu"], ordered.Select(x => x.Id));

        var menu = Assert.Single(await new SettingsMenu(registry).GetMenuItemsAsync());
        Assert.Equal(MenuItemGroups.Administration.Name, menu.GroupName);
        Assert.Equal(900, menu.Order);
        Assert.Equal(["Overview", "Alpha", "Zulu"], menu.SubMenuItems.Select(x => x.Text));

        var page = Render<SettingsPage>();
        page.WaitForAssertion(() =>
        {
            Assert.Contains("Open Alpha", page.Markup);
            Assert.Contains("Open Zulu", page.Markup);
            Assert.True(page.Markup.IndexOf("Open Alpha", StringComparison.Ordinal) <
                        page.Markup.IndexOf("Open Zulu", StringComparison.Ordinal));
        });
    }

    [Fact]
    public async Task DuplicateSectionIdsFailInsteadOfCreatingAmbiguousNavigation()
    {
        var registry = new SettingsSectionRegistry(
        [
            new Provider([Section("duplicate", "One", 0)]),
            new Provider([Section("DUPLICATE", "Two", 1)])
        ]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await registry.ListAsync());
        Assert.Contains("registered more than once", exception.Message);
    }

    private static SettingsSectionDescriptor Section(string id, string name, float order) =>
        new(id, name, $"{name} settings", $"settings/{id}", Icons.Material.Outlined.Settings, order);

    private sealed class Provider(IReadOnlyCollection<SettingsSectionDescriptor> sections) : ISettingsSectionProvider
    {
        public ValueTask<IEnumerable<SettingsSectionDescriptor>> GetSectionsAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IEnumerable<SettingsSectionDescriptor>>(sections);
    }
}
