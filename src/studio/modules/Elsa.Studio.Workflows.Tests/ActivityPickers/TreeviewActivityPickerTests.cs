using Bunit;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.ActivityPickers.Treeview;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests.ActivityPickers;

public sealed class TreeviewActivityPickerTests : BunitContext, IAsyncLifetime
{
    public TreeviewActivityPickerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IActivityRegistry>(new ActivityRegistryStub(
        [
            new ActivityDescriptor
            {
                Name = "WriteLine",
                DisplayName = "Write Line",
                TypeName = "Elsa.WriteLine",
                Category = "Flow",
                IsBrowsable = true
            }
        ]));
        Services.AddSingleton<IActivityDisplaySettingsRegistry>(new ActivityDisplaySettingsRegistryStub());
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void ClickingCategoryLabel_TogglesItsExpansion()
    {
        Render<MudPopoverProvider>();
        var cut = Render<ActivityPicker>();
        var category = cut.FindComponents<MudTreeViewItem<string>>().Single(item => item.Instance.Text == "Flow");

#pragma warning disable MUD0012 // The component is the system under test; Expanded is its observable state.
        Assert.False(category.Instance.Expanded);

        category.Find(".mud-treeview-item-label").Click();

        cut.WaitForAssertion(() => Assert.True(cut.FindComponents<MudTreeViewItem<string>>().Single(item => item.Instance.Text == "Flow").Instance.Expanded));

        cut.FindComponents<MudTreeViewItem<string>>().Single(item => item.Instance.Text == "Flow").Find(".mud-treeview-item-label").Click();

        cut.WaitForAssertion(() => Assert.False(cut.FindComponents<MudTreeViewItem<string>>().Single(item => item.Instance.Text == "Flow").Instance.Expanded));
#pragma warning restore MUD0012
    }

    private sealed class ActivityRegistryStub(IEnumerable<ActivityDescriptor> activities) : IActivityRegistry
    {
        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IEnumerable<ActivityDescriptor> List() => activities;
        public ActivityDescriptor? Find(string activityType, int? version = null) => activities.FirstOrDefault(x => x.TypeName == activityType);
        public IEnumerable<ActivityDescriptor> FindAll(string activityType) => activities.Where(x => x.TypeName == activityType);
        public void MarkStale()
        {
        }
    }

    private sealed class ActivityDisplaySettingsRegistryStub : IActivityDisplaySettingsRegistry
    {
        public ActivityDisplaySettings GetSettings(string activityType) => new("#0EA5E9");
        public void MarkStale()
        {
        }
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
