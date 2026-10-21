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
    public void HidesCompensationScopeWhenFreshCapabilityIsUnavailableButKeepsOrdinaryActivities()
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
    }

    [Fact]
    public async Task RechecksNativeCapabilityBeforeDragAndAllowsOrdinaryActivitiesAfterContextChange()
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

        await cut.InvokeAsync(() => InvokeDragStartAsync(cut.Instance, _compensationScope));

        Assert.Null(dragDropManager.Payload);
        await cut.InvokeAsync(() => InvokeDragStartAsync(cut.Instance, _ordinaryActivity));
        Assert.Same(_ordinaryActivity, dragDropManager.Payload);
        Assert.Equal(2, _featureProvider.FreshCheckCount);
    }

    [Fact]
    public async Task LaterOrdinaryDragKeepsPayloadWhenAnEarlierNativeCheckCompletes()
    {
        _featureProvider.Enabled = true;
        Render<MudPopoverProvider>();
        var cut = Render<ActivityPicker>();
        var dragDropManager = new DragDropManager();
        cut.Instance.DragDropManager = dragDropManager;
        var pendingCheck = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _featureProvider.DeferredCheck = pendingCheck;

        var nativeDrag = cut.InvokeAsync(() => InvokeDragStartAsync(cut.Instance, _compensationScope));
        await _featureProvider.DeferredCheckStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await cut.InvokeAsync(() => InvokeDragStartAsync(cut.Instance, _ordinaryActivity));
        Assert.Same(_ordinaryActivity, dragDropManager.Payload);

        pendingCheck.SetResult(true);
        await nativeDrag;

        Assert.Same(_ordinaryActivity, dragDropManager.Payload);
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

    private static Task InvokeDragStartAsync(ActivityPicker picker, ActivityDescriptor descriptor) =>
        (Task)typeof(ActivityPicker).GetMethod("OnDragStart", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(picker, [descriptor])!;

    private sealed class MutableFreshFeatureProvider(bool enabled) : IRemoteFeatureProvider, IRemoteFeatureProviderWithFreshCheck
    {
        public TaskCompletionSource<bool>? DeferredCheck { get; set; }
        public TaskCompletionSource DeferredCheckStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Enabled { get; set; } = enabled;
        public int FreshCheckCount { get; private set; }

        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(Enabled);
        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<FeatureDescriptor>>([]);

        public Task<bool> IsEnabledFreshAsync(string featureName, CancellationToken cancellationToken = default)
        {
            FreshCheckCount++;
            if (DeferredCheck is { } deferredCheck)
            {
                DeferredCheck = null;
                DeferredCheckStarted.TrySetResult();
                return deferredCheck.Task;
            }

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
