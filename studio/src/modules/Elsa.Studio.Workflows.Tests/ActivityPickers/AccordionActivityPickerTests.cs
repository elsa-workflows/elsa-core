using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.ActivityPickers.Accordion;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Models;
using Xunit;

namespace Elsa.Studio.Workflows.Tests.ActivityPickers;

public sealed class AccordionActivityPickerTests
{
    [Fact]
    public async Task LaterOrdinaryDragKeepsPayloadWhenAnEarlierNativeCheckCompletes()
    {
        var nativeScope = new ActivityDescriptor { TypeName = "Elsa.CompensationScope" };
        var ordinaryActivity = new ActivityDescriptor { TypeName = "Elsa.WriteLine" };
        var dragDropManager = new DragDropManager();
        var featureProvider = new DeferredFreshFeatureProvider();
        var picker = new ActivityPicker { DragDropManager = dragDropManager };
        SetPrivateProperty(picker, "RemoteFeatureProvider", featureProvider);
        SetPrivateProperty(picker, "BackendApiClientProvider", new MutableBackendApiClientProvider("context-A"));

        var nativeCheck = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        featureProvider.DeferredCheck = nativeCheck;
        var nativeDrag = InvokeDragStartAsync(picker, nativeScope);
        await featureProvider.DeferredCheckStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await InvokeDragStartAsync(picker, ordinaryActivity);
        Assert.Same(ordinaryActivity, dragDropManager.Payload);

        nativeCheck.SetResult(true);
        await nativeDrag;

        Assert.Same(ordinaryActivity, dragDropManager.Payload);
        Assert.Equal(1, featureProvider.FreshCheckCount);
    }

    [Fact]
    public async Task OrdinaryDragDoesNotReadBackendContext()
    {
        var ordinaryActivity = new ActivityDescriptor { TypeName = "Elsa.WriteLine" };
        var dragDropManager = new DragDropManager();
        var picker = new ActivityPicker { DragDropManager = dragDropManager };
        var backendProvider = new MutableBackendApiClientProvider("context-A") { ThrowOnContextRead = true };
        SetPrivateProperty(picker, "RemoteFeatureProvider", new DeferredFreshFeatureProvider());
        SetPrivateProperty(picker, "BackendApiClientProvider", backendProvider);

        await InvokeDragStartAsync(picker, ordinaryActivity);

        Assert.Same(ordinaryActivity, dragDropManager.Payload);
    }

    private static void SetPrivateProperty(ActivityPicker component, string propertyName, object value) =>
        typeof(ActivityPicker).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(component, value);

    private static Task InvokeDragStartAsync(ActivityPicker picker, ActivityDescriptor descriptor) =>
        (Task)typeof(ActivityPicker).GetMethod("OnDragStart", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(picker, [descriptor])!;

    private sealed class DeferredFreshFeatureProvider : IRemoteFeatureProvider, IRemoteFeatureProviderWithFreshCheck
    {
        public TaskCompletionSource<bool>? DeferredCheck { get; set; }
        public TaskCompletionSource DeferredCheckStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int FreshCheckCount { get; private set; }

        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(false);
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

            return Task.FromResult(false);
        }
    }

    private sealed class MutableBackendApiClientProvider(string contextKey) : IBackendApiClientProvider, IBackendRequestContextBinding
    {
        private string _contextKey = contextKey;
        public Uri Url { get; } = new("https://backend.example/");
        public string ContextKey
        {
            get => ThrowOnContextRead ? throw new InvalidOperationException("Ordinary activity drags must not inspect backend context.") : _contextKey;
            set => _contextKey = value;
        }
        public bool ThrowOnContextRead { get; set; }
        public bool SupportsAuthenticatedRequestContextBinding => true;
        public bool HasActiveRequestContextBinding => false;

        public void RegisterAuthenticatedRequestTransport(IBackendRequestContextEpochSource? epochSource) { }
        public IDisposable BindRequestContext(string expectedContextKey) => throw new NotSupportedException();
        public void ValidateBoundRequestContext(IBackendRequestContextEpochSource? requestEpochSource) { }

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class =>
            throw new NotSupportedException();
    }
}
