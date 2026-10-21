using Bunit;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.ActivityPickers.Treeview;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using Elsa.Studio.Testing;

namespace Elsa.Studio.Workflows.Tests.ActivityPickers;

public sealed class TreeviewActivityPickerTests : BunitContext, IAsyncLifetime
{
    private readonly MutableFreshFeatureProvider _featureProvider = new(false);
    private readonly MutableBackendApiClientProvider _backendProvider = new("context-A");
    private readonly ActivityDescriptor _ordinaryActivity = new()
    {
        Name = "WriteLine",
        DisplayName = "Write Line",
        TypeName = "Elsa.WriteLine",
        Category = "Flow",
        IsBrowsable = true
    };
    private readonly ActivityDescriptor _compensationScope = new()
    {
        Name = "CompensationScope",
        DisplayName = "Compensation Scope",
        TypeName = "Elsa.CompensationScope",
        Category = "Flow",
        IsBrowsable = true
    };

    public TreeviewActivityPickerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IActivityRegistry>(new ActivityRegistryStub(
        [
            _ordinaryActivity,
            _compensationScope
        ]));
        Services.AddSingleton<IActivityDisplaySettingsRegistry>(new ActivityDisplaySettingsRegistryStub());
        Services.AddSingleton<IRemoteFeatureProvider>(_featureProvider);
        Services.AddSingleton<IBackendApiClientProvider>(_backendProvider);
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task HidesCompensationScopeWhenFreshCapabilityIsUnavailableButKeepsOrdinaryActivities()
    {
        Render<MudPopoverProvider>();
        var cut = Render<ActivityPicker>();
        var flow = cut.FindComponents<MudTreeViewItem<string>>().Single(item => item.Instance.Text == "Flow");
        flow.Find(".mud-treeview-item-label").Click();

        cut.WaitForAssertion(() =>
        {
            var leafNames = cut.FindComponents<MudTreeViewItem<string>>().Select(item => item.Instance.Text).ToArray();
            Assert.Contains("Write Line", leafNames);
            Assert.DoesNotContain("Compensation Scope", leafNames);
        });

        var dragDropManager = new DragDropManager { Payload = "stale payload" };
        cut.Instance.DragDropManager = dragDropManager;
        await cut.InvokeAsync(() => InvokeDragStartAsync(cut.Instance, _compensationScope));
        Assert.Null(dragDropManager.Payload);
    }

    [Fact]
    public async Task UsesFilteredNativeMembershipForSynchronousDragAndKeepsOrdinaryDragIndependent()
    {
        _featureProvider.Enabled = true;
        Render<MudPopoverProvider>();
        var cut = Render<ActivityPicker>();
        var flow = cut.FindComponents<MudTreeViewItem<string>>().Single(item => item.Instance.Text == "Flow");
        flow.Find(".mud-treeview-item-label").Click();
        cut.WaitForAssertion(() => Assert.Contains(
            cut.FindComponents<MudTreeViewItem<string>>(), item => item.Instance.Text == "Compensation Scope"));

        var dragDropManager = new DragDropManager { Payload = "stale payload" };
        cut.Instance.DragDropManager = dragDropManager;
        _featureProvider.Enabled = false;
        _backendProvider.ContextKey = "context-B";

        var nativeDrag = InvokeDragStartAsync(cut.Instance, _compensationScope);
        Assert.True(nativeDrag.IsCompleted);
        await cut.InvokeAsync(() => nativeDrag);

        Assert.Same(_compensationScope, dragDropManager.Payload);
        await cut.InvokeAsync(() => InvokeDragStartAsync(cut.Instance, _ordinaryActivity));
        Assert.Same(_ordinaryActivity, dragDropManager.Payload);
        Assert.Equal(1, _featureProvider.FreshCheckCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CategoryDragStartDoesNotClearTheLeafPayload(bool native)
    {
        _featureProvider.Enabled = native;
        Render<MudPopoverProvider>();
        var cut = Render<ActivityPicker>();
        var dragDropManager = new DragDropManager();
        cut.Instance.DragDropManager = dragDropManager;
        var descriptor = native ? _compensationScope : _ordinaryActivity;

        await cut.InvokeAsync(() => InvokeDragStartAsync(cut.Instance, descriptor));
        Assert.Same(descriptor, dragDropManager.Payload);

        // MudTreeViewItem raises dragstart on its parent category as the event bubbles.
        await cut.InvokeAsync(() => InvokeDragStartAsync(cut.Instance, null));

        Assert.Same(descriptor, dragDropManager.Payload);
    }

    [Fact]
    public async Task NativeDragstartDoesNotWaitForAnotherFeatureCheck()
    {
        _featureProvider.Enabled = true;
        Render<MudPopoverProvider>();
        var cut = Render<ActivityPicker>();
        var dragDropManager = new DragDropManager();
        cut.Instance.DragDropManager = dragDropManager;
        var nativeDrag = InvokeDragStartAsync(cut.Instance, _compensationScope);
        Assert.True(nativeDrag.IsCompleted);
        await cut.InvokeAsync(() => nativeDrag);
        Assert.Same(_compensationScope, dragDropManager.Payload);

        await cut.InvokeAsync(() => InvokeDragStartAsync(cut.Instance, _ordinaryActivity));
        Assert.Same(_ordinaryActivity, dragDropManager.Payload);
        Assert.Same(_ordinaryActivity, dragDropManager.Payload);
        Assert.Equal(1, _featureProvider.FreshCheckCount);
    }

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

    private static Task InvokeDragStartAsync(ActivityPicker picker, ActivityDescriptor? descriptor) =>
        (Task)typeof(ActivityPicker).GetMethod("OnDragStart", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(picker, [descriptor])!;

    private sealed class MutableFreshFeatureProvider(bool enabled) : IRemoteFeatureProvider, IRemoteFeatureProviderWithFreshCheck
    {
        public bool Enabled { get; set; } = enabled;
        public int FreshCheckCount { get; private set; }

        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(Enabled);
        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<FeatureDescriptor>>([]);

        public Task<bool> IsEnabledFreshAsync(string featureName, CancellationToken cancellationToken = default)
        {
            FreshCheckCount++;
            return Task.FromResult(Enabled);
        }
    }

    private sealed class MutableBackendApiClientProvider(string contextKey) : IBackendApiClientProvider, IBackendRequestContextBinding
    {
        public Uri Url { get; } = new("https://backend.example/");
        public string ContextKey { get; set; } = contextKey;
        public bool SupportsAuthenticatedRequestContextBinding => true;
        public bool HasActiveRequestContextBinding => false;

        public void RegisterAuthenticatedRequestTransport(IBackendRequestContextEpochSource? epochSource) { }
        public IDisposable BindRequestContext(string expectedContextKey) => throw new NotSupportedException();
        public void ValidateBoundRequestContext(IBackendRequestContextEpochSource? requestEpochSource) { }

        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class =>
            throw new NotSupportedException();
    }

}
